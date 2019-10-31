using System.Threading.Tasks;
using Xamarin.Essentials;
using Xunit;

namespace Tests
{
    public class Calendar_Tests
    {
        [Fact]
        public void Calendar_IsSupported_Fail_On_NetStandard() =>
            Assert.Throws<NotImplementedInReferenceAssemblyException>(() => Calendar.IsSupported);
    }
}
