using System;
using Xunit;

namespace Tests.Selenium.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireEnvironmentVariablesFactAttribute : FactAttribute
    {
        public RequireEnvironmentVariablesFactAttribute(params string[] names)
        {
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
                {
                    Skip = $"Environment variable '{name}' is not set.";
                    return;
                }
            }
        }
    }
}