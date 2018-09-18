using Elastic.Xunit.XunitPlumbing;
using FluentAssertions;

namespace Elastic.Xunit.ExampleComplex
{
	public class InheritYetAgainTestsClass : InheritTestsClass
	{
		public InheritYetAgainTestsClass(TestCluster cluster) : base(cluster) { }

		[I] public void TestOnClassItselfAgain()
		{
			var info = this.Client.RootNodeInfo();

			info.IsValid.Should().BeTrue();

		}
	}
}
