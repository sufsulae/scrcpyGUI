using System;
using System.Text.RegularExpressions;

namespace scrcpyGUI
{
    public static class Util {
        static Regex regexNumberOnly = new Regex("[^0-9]+");

        public static bool isStringNumberOnly(string str) {
            return regexNumberOnly.IsMatch(str);
        }
    }
}
