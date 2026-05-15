using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Helper
{
    public class Security
    {
        ///<summary>
        ///Using for encode string before render to view or sanitize result. Including library AntiXss.AntiXssEncoder in this function.
        ///</summary>
        public string HtmlEncode(string input, bool useNamedEntities)
        {
            string _input = "";

            if (!string.IsNullOrWhiteSpace(input) && input != "")
            {
                try
                {
                    _input = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(Regex.Replace(input, @"[^0-9a-zA-Z\:\;\,\.\-\+\=\*\&\^\%\$\#\@\!\?\(\)\|\W_\ ]+", ""), useNamedEntities);
                }
                catch (Exception er)
                {
                }
            }
            return _input.Trim();
        }

        ///<summary>
        ///Using for encode string before render to view or sanitize result. Including library AntiXss.AntiXssEncoder in this function.
        ///</summary>
        public string SanitizeToView(string input, bool useNamedEntities)
        {
            string _input = "";

            if (!string.IsNullOrWhiteSpace(input) && input != "")
            {
                try
                {
                    _input = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(Regex.Replace(input, @"[^0-9a-zA-Z\:\;\,\.\-\+\=\*\&\^\%\$\#\@\!\?\(\)\|\W_\ ]+", ""), useNamedEntities);
                }
                catch (Exception er)
                {
                }
            }
            return _input.Trim();
        }

        ///<summary>
        ///Using for encode string before render to view or sanitize result. Including library AntiXss.AntiXssEncoder in this function.
        ///</summary>
        public string SanitizeToViewOnly(string input)
        {
            string _input = "";

            if (!string.IsNullOrWhiteSpace(input) && input != "")
            {
                try
                {
                    _input = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(input, true);
                }
                catch (Exception er)
                {
                }
            }
            return _input.Trim();
        }

        public string RegexInputToSystem(object input)
        {
            string _input = "";
            if (input == null)
            {
                _input = string.Empty;
            }
            else
            {
                _input = input.ToString();
            }
            if (!string.IsNullOrEmpty(_input))
            {
                try
                {
                    _input = Regex.Replace(_input, @"[^0-9a-zA-Z\:\;\,\.\-\+\=\*\&\^\%\$\#\@\!\?\(\)\|\W_\ ]+", "");
                }
                catch (Exception er)
                {
                }
            }
            return _input.Trim();
        }

        ///<summary>
        ///Using for encode string before render to view or for sanitize input ID from request. Including library AntiXss.AntiXssEncoder in this function.
        ///</summary>
        public string RegexForId(string input)
        {
            string _input = "0";
            if (!string.IsNullOrWhiteSpace(input) && input != "")
            {
                try
                {
                    _input = System.Web.Security.AntiXss.AntiXssEncoder.HtmlEncode(Regex.Replace(input, @"[^0-9]+", ""), false);
                }
                catch (Exception er)
                {
                }
            }

            return _input.Trim();
        }
        ///<summary>
        ///Using for sanitize input from request. Including library AntiXss.AntiXssEncoder in this function.
        ///</summary>
        public string RegexInputToSystem(string input)
        {
            string _input = "";
            if (!string.IsNullOrWhiteSpace(input) && input != "")
            {
                try
                {
                    _input = Regex.Replace(input, @"[^0-9a-zA-Z\:\;\,\.\-\\_\<\>\[\]\+\=\*\&\^\%\$\#\@\!\?\(\)\|\\\/\ ]+", "");
                }
                catch (Exception er)
                {
                }
            }

            return _input.Trim();
        }
        ///<summary>
        ///Using for sanitize input from request. Including library AntiXss.AntiXssEncoder in this function.
        ///</summary>
        public string RegexIdToSystem(string input)
        {
            string _input = "";
            if (!string.IsNullOrWhiteSpace(input) && input != "")
            {
                try
                {
                    _input = Regex.Replace(input, @"[^0-9]+", "");
                }
                catch (Exception er)
                {
                }
            }

            return _input.Trim();
        }
        ///<summary>
        ///Using for sanitize input from request. Including library AntiXss.AntiXssEncoder in this function.
        ///</summary>
        public string RegexInputCurrencyToSystem(string input)
        {
            string _input = "";
            if (!string.IsNullOrWhiteSpace(input) && input != "")
            {
                try
                {
                    _input = Regex.Replace(input, @"[^0-9\,\.]+", "");
                }
                catch (Exception er)
                {
                }
            }

            return _input.Trim();
        }
        private string GeneratetRandomStringForUrl()
        {
            string cryptRandom = string.Empty;

            var r = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] data = new byte[4];
            r.GetBytes(data);
            Int32 vl = BitConverter.ToInt32(data, 0);
            if (vl < 0) vl = -vl;

            if (!string.IsNullOrWhiteSpace(vl.ToString()) && vl.ToString() != "")
            {
                return vl.ToString();
            }
            return Guid.NewGuid().ToString();
        }
        public string GetRandomStringForUrl()
        {
            return SanitizeToView(GeneratetRandomStringForUrl(), false);
        }

    }
}
