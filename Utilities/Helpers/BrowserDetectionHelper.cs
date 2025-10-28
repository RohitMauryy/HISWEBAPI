using System.Text.RegularExpressions;

namespace HISWEBAPI.Utilities
{
    public static class BrowserDetectionHelper
    {
        public static (string browser, string version, string os, string device, string deviceType) ParseUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return ("Unknown", "Unknown", "Unknown", "Unknown", "Unknown");

            var browser = DetectBrowser(userAgent);
            var browserVersion = DetectBrowserVersion(userAgent, browser);
            var os = DetectOperatingSystem(userAgent);
            var device = DetectDevice(userAgent);
            var deviceType = DetectDeviceType(userAgent);

            return (browser, browserVersion, os, device, deviceType);
        }

        private static string DetectBrowser(string userAgent)
        {
            if (userAgent.Contains("Edg/")) return "Edge";
            if (userAgent.Contains("Chrome/") && !userAgent.Contains("Edg/")) return "Chrome";
            if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/")) return "Safari";
            if (userAgent.Contains("Firefox/")) return "Firefox";
            if (userAgent.Contains("MSIE") || userAgent.Contains("Trident/")) return "Internet Explorer";
            if (userAgent.Contains("Opera") || userAgent.Contains("OPR/")) return "Opera";
            return "Unknown";
        }

        private static string DetectBrowserVersion(string userAgent, string browser)
        {
            try
            {
                string pattern = browser switch
                {
                    "Edge" => @"Edg/([\d.]+)",
                    "Chrome" => @"Chrome/([\d.]+)",
                    "Safari" => @"Version/([\d.]+)",
                    "Firefox" => @"Firefox/([\d.]+)",
                    "Opera" => @"(?:Opera|OPR)/([\d.]+)",
                    "Internet Explorer" => @"(?:MSIE |rv:)([\d.]+)",
                    _ => null
                };

                if (pattern == null) return "Unknown";

                var match = Regex.Match(userAgent, pattern);
                return match.Success ? match.Groups[1].Value : "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string DetectOperatingSystem(string userAgent)
        {
            if (userAgent.Contains("Windows NT 10.0")) return "Windows 10";
            if (userAgent.Contains("Windows NT 11.0")) return "Windows 11";
            if (userAgent.Contains("Windows NT 6.3")) return "Windows 8.1";
            if (userAgent.Contains("Windows NT 6.2")) return "Windows 8";
            if (userAgent.Contains("Windows NT 6.1")) return "Windows 7";
            if (userAgent.Contains("Mac OS X")) return ExtractMacVersion(userAgent);
            if (userAgent.Contains("Android")) return ExtractAndroidVersion(userAgent);
            if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return ExtractIOSVersion(userAgent);
            if (userAgent.Contains("Linux")) return "Linux";
            if (userAgent.Contains("Ubuntu")) return "Ubuntu";
            return "Unknown";
        }

        private static string ExtractMacVersion(string userAgent)
        {
            var match = Regex.Match(userAgent, @"Mac OS X ([\d_]+)");
            if (match.Success)
            {
                var version = match.Groups[1].Value.Replace('_', '.');
                return $"Mac OS X {version}";
            }
            return "Mac OS X";
        }

        private static string ExtractAndroidVersion(string userAgent)
        {
            var match = Regex.Match(userAgent, @"Android ([\d.]+)");
            return match.Success ? $"Android {match.Groups[1].Value}" : "Android";
        }

        private static string ExtractIOSVersion(string userAgent)
        {
            var match = Regex.Match(userAgent, @"OS ([\d_]+)");
            if (match.Success)
            {
                var version = match.Groups[1].Value.Replace('_', '.');
                return $"iOS {version}";
            }
            return "iOS";
        }

        private static string DetectDevice(string userAgent)
        {
            // Mobile devices
            if (userAgent.Contains("iPhone")) return "iPhone";
            if (userAgent.Contains("iPad")) return "iPad";
            if (userAgent.Contains("iPod")) return "iPod";

            // Android devices
            var androidMatch = Regex.Match(userAgent, @"Android.*?;\s*(.*?)\s+Build");
            if (androidMatch.Success) return androidMatch.Groups[1].Value;

            // Samsung
            if (userAgent.Contains("SM-"))
            {
                var samsungMatch = Regex.Match(userAgent, @"(SM-[A-Z0-9]+)");
                if (samsungMatch.Success) return samsungMatch.Groups[1].Value;
            }

            // Desktop
            if (userAgent.Contains("Windows") || userAgent.Contains("Macintosh") || userAgent.Contains("Linux"))
                return "Desktop";

            return "Unknown";
        }

        private static string DetectDeviceType(string userAgent)
        {
            if (userAgent.Contains("Mobile") || userAgent.Contains("Android") ||
                userAgent.Contains("iPhone") || userAgent.Contains("iPod"))
                return "Mobile";

            if (userAgent.Contains("iPad") || userAgent.Contains("Tablet"))
                return "Tablet";

            return "Desktop";
        }
    }
}