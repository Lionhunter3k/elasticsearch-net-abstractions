using Elastic.Xunit.XunitPlumbing;
using FluentAssertions;

namespace Elastic.Xunit.ExampleComplex
{
	public class InheritTestsClass : TestingClass
	{
		public InheritTestsClass(TestCluster cluster) : base(cluster) { }

		[I] public void AdditionalTest()
		{
			var info = this.Client.RootNodeInfo();

			info.IsValid.Should().BeTrue();

		}
	}
}
