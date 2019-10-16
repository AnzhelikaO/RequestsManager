using System;
namespace RequestsManagerAPI
{
    public class KeyNotConfiguredException : Exception
    {
        private string Key;
        public KeyNotConfiguredException(string Key) =>
            this.Key = Key;
        public override string Message => $"Key '{Key}' is not configured.";
    }
}