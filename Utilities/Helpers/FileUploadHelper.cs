using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using log4net;
using System.Reflection;

namespace HISWEBAPI.Utilities
{
    /// <summary>
    /// Helper class for handling file uploads to DMS (Document Management System)
    /// </summary>
    public class FileUploadHelper
    {
        private readonly IConfiguration _configuration;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public FileUploadHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Upload a file from IFormFile (multipart/form-data) to the DMS folder structure
        /// </summary>
        /// <param name="file">IFormFile from the request</param>
        /// <param name="subFolderType">Type of subfolder (LetterHeadImages, PatientImages, DoctorSignatures, etc.)</param>
        /// <returns>Tuple of (success, filePath, errorMessage)</returns>
        public (bool success, string filePath, string errorMessage) UploadFile(IFormFile file, string subFolderType)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _log.Warn("File is null or empty");
                    return (false, null, "No file provided or file is empty");
                }

                // Validate file size
                int maxSizeMB = _configuration.GetValue<int>("DMS:MaxFileSizeMB", 25);
                double fileSizeMB = file.Length / (1024.0 * 1024.0);

                if (fileSizeMB > maxSizeMB)
                {
                    _log.Warn($"File size ({fileSizeMB:F2} MB) exceeds maximum ({maxSizeMB} MB)");
                    return (false, null, $"File size ({fileSizeMB:F2} MB) exceeds maximum allowed size ({maxSizeMB} MB)");
                }

                // Get file extension
                string fileExtension = Path.GetExtension(file.FileName)?.TrimStart('.').ToLower();
                if (string.IsNullOrEmpty(fileExtension))
                {
                    _log.Error("File has no extension");
                    return (false, null, "File must have a valid extension");
                }

                // Validate file extension
                if (!IsValidExtension(fileExtension))
                {
                    _log.Warn($"Invalid file extension: {fileExtension}");
                    return (false, null, $"File type .{fileExtension} is not allowed");
                }

                // Get base DMS path from configuration
                string baseDmsPath = _configuration.GetValue<string>("DMS:RootPath") ?? "D:\\DMS";

                // Create subfolder path
                string subFolderPath = Path.Combine(baseDmsPath, subFolderType);
                if (!Directory.Exists(subFolderPath))
                {
                    Directory.CreateDirectory(subFolderPath);
                    _log.Info($"Created directory: {subFolderPath}");
                }

                // Generate unique filename
                string fileName = GenerateUniqueFileName(fileExtension);
                string fullFilePath = Path.Combine(subFolderPath, fileName);

                // Save file to disk
                using (var fileStream = new FileStream(fullFilePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                _log.Info($"File uploaded successfully: {fullFilePath}, Size: {fileSizeMB:F2} MB");

                // Return path with forward slashes for database storage
                string dbFilePath = fullFilePath.Replace("\\", "/");
                return (true, dbFilePath, null);
            }
            catch (Exception ex)
            {
                _log.Error($"Error uploading file: {ex.Message}", ex);
                return (false, null, $"File upload failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Upload a file (image/pdf/excel) to the DMS folder structure from base64
        /// </summary>
        /// <param name="base64Data">Base64 encoded file data with data URI prefix (e.g., "data:image/jpeg;base64,...")</param>
        /// <param name="subFolderType">Type of subfolder (LetterHeadImages, PatientImages, DoctorSignatures, etc.)</param>
        /// <returns>Tuple of (success, filePath, errorMessage)</returns>
        public (bool success, string filePath, string errorMessage) UploadFileFromBase64(string base64Data, string subFolderType)
        {
            try
            {
                if (string.IsNullOrEmpty(base64Data))
                {
                    _log.Warn("Base64 data is null or empty");
                    return (false, null, "File data is required");
                }

                // Get base DMS path from configuration
                string baseDmsPath = _configuration.GetValue<string>("DMS:RootPath") ?? "D:\\DMS";

                // Parse base64 data
                var (fileBytes, fileExtension, parseError) = ParseBase64Data(base64Data);
                if (!string.IsNullOrEmpty(parseError))
                {
                    _log.Error($"Failed to parse base64 data: {parseError}");
                    return (false, null, parseError);
                }

                // Create subfolder path
                string subFolderPath = Path.Combine(baseDmsPath, subFolderType);
                if (!Directory.Exists(subFolderPath))
                {
                    Directory.CreateDirectory(subFolderPath);
                    _log.Info($"Created directory: {subFolderPath}");
                }

                // Generate unique filename
                string fileName = GenerateUniqueFileName(fileExtension);
                string fullFilePath = Path.Combine(subFolderPath, fileName);

                // Write file to disk
                File.WriteAllBytes(fullFilePath, fileBytes);
                _log.Info($"File uploaded successfully: {fullFilePath}");

                // Return path with forward slashes for database storage
                string dbFilePath = fullFilePath.Replace("\\", "/");
                return (true, dbFilePath, null);
            }
            catch (Exception ex)
            {
                _log.Error($"Error uploading file: {ex.Message}", ex);
                return (false, null, $"File upload failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse base64 data and extract file bytes and extension
        /// </summary>
        private (byte[] fileBytes, string extension, string errorMessage) ParseBase64Data(string base64Data)
        {
            try
            {
                // Split the data URI: "data:image/jpeg;base64,/9j/4AAQ..."
                var parts = base64Data.Split(new[] { ";base64," }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    return (null, null, "Invalid base64 data format. Expected format: 'data:mime/type;base64,DATA'");
                }

                // Extract MIME type and convert to extension
                string mimeType = parts[0].Replace("data:", "");
                string extension = GetExtensionFromMimeType(mimeType);

                if (string.IsNullOrEmpty(extension))
                {
                    return (null, null, $"Unsupported MIME type: {mimeType}");
                }

                // Decode base64 string to bytes
                byte[] fileBytes = Convert.FromBase64String(parts[1]);

                return (fileBytes, extension, null);
            }
            catch (FormatException)
            {
                return (null, null, "Invalid base64 encoding");
            }
            catch (Exception ex)
            {
                return (null, null, $"Error parsing base64 data: {ex.Message}");
            }
        }

        private string GetExtensionFromMimeType(string mimeType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate if file extension is allowed
        /// </summary>
        private bool IsValidExtension(string extension)
        {
            var allowedExtensions = new[]
            {
                "jpg", "jpeg", "png", "gif", "bmp", "webp", "svg",
                "pdf", "doc", "docx", "xls", "xlsx", "txt", "zip"
            };

            return allowedExtensions.Contains(extension.ToLower());
        }

        /// <summary>
        /// Generate a unique filename using timestamp and random hash
        /// </summary>
        private string GenerateUniqueFileName(string extension)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string randomHash = GenerateRandomHash(8);
            return $"{timestamp}_{randomHash}.{extension}";
        }

        /// <summary>
        /// Generate a random hash string
        /// </summary>
        private string GenerateRandomHash(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Delete a file from DMS
        /// </summary>
        public bool DeleteFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                // Convert forward slashes back to backslashes for Windows
                string actualPath = filePath.Replace("/", "\\");

                if (File.Exists(actualPath))
                {
                    File.Delete(actualPath);
                    _log.Info($"File deleted successfully: {actualPath}");
                    return true;
                }
                else
                {
                    _log.Warn($"File not found for deletion: {actualPath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error deleting file: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Validate file size for IFormFile
        /// </summary>
        public (bool isValid, string errorMessage) ValidateFileSize(IFormFile file, int maxSizeMB = 25)
        {
            try
            {
                if (file == null)
                {
                    return (false, "No file provided");
                }

                double fileSizeMB = file.Length / (1024.0 * 1024.0);

                if (fileSizeMB > maxSizeMB)
                {
                    return (false, $"File size ({fileSizeMB:F2} MB) exceeds maximum allowed size ({maxSizeMB} MB)");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _log.Error($"Error validating file size: {ex.Message}", ex);
                return (false, "Error validating file size");
            }
        }

        /// <summary>
        /// Validate file size for base64 (legacy support)
        /// </summary>
        public (bool isValid, string errorMessage) ValidateBase64FileSize(string base64Data, int maxSizeMB = 25)
        {
            try
            {
                var parts = base64Data.Split(new[] { ";base64," }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    return (false, "Invalid file format");
                }

                byte[] fileBytes = Convert.FromBase64String(parts[1]);
                double fileSizeMB = fileBytes.Length / (1024.0 * 1024.0);

                if (fileSizeMB > maxSizeMB)
                {
                    return (false, $"File size ({fileSizeMB:F2} MB) exceeds maximum allowed size ({maxSizeMB} MB)");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _log.Error($"Error validating file size: {ex.Message}", ex);
                return (false, "Error validating file size");
            }
        }
    }
}