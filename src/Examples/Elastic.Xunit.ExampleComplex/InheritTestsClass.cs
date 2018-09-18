using Elastic.Xunit.XunitPlumbing;
using FluentAssertions;

namespace Elastic.Xunit.ExampleComplex
{
	public class InheritTestsClass : MyTestClass
	{
		public InheritTestsClass(TestCluster cluster) : base(cluster) { }

		[I] public void TestOnClassItself()
		{
			var info = this.Client.RootNodeInfo();

			info.IsValid.Should().BeTrue();

		}
	}
}