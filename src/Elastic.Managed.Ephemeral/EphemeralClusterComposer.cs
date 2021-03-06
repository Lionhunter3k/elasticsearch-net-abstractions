using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Elastic.Managed.ConsoleWriters;
using Elastic.Managed.Ephemeral.Tasks;
using Elastic.Managed.Ephemeral.Tasks.AfterNodeStoppedTasks;
using Elastic.Managed.Ephemeral.Tasks.BeforeStartNodeTasks;
using Elastic.Managed.Ephemeral.Tasks.BeforeStartNodeTasks.XPack;
using Elastic.Managed.Ephemeral.Tasks.InstallationTasks;
using Elastic.Managed.Ephemeral.Tasks.InstallationTasks.XPack;
using Elastic.Managed.Ephemeral.Tasks.ValidationTasks;
using Elastic.Managed.FileSystem;

namespace Elastic.Managed.Ephemeral
{
	public class EphemeralClusterComposerBase
	{
		protected EphemeralClusterComposerBase() { }

		internal static IEnumerable<IClusterComposeTask> InstallationTasks { get; } = new List<IClusterComposeTask>
		{
			new CreateLocalApplicationDirectory(),
			new CopyCachedEsInstallation(),
			new EnsureJavaHomeEnvironmentVariableIsSet(),
			new DownloadElasticsearchVersion(),
			new UnzipElasticsearch(),
			new InstallPlugins(),

			new EnsureElasticsearchBatWorksAcrossDrives(),
			new EnsureXPackEnvBinaryExists(),
			new PathXPackInBatFile()
		};

		protected static IEnumerable<IClusterComposeTask> BeforeStart { get; } = new List<IClusterComposeTask>
		{
			new CreateEphemeralDirectory(),
			new EnsureSecurityRealms(),
			new EnsureSecurityRolesFileExists(),

			new EnsureSecurityUsersInDefaultRealmAreAdded(),
			new GenerateCertificatesTask(),
			new AddClientCertificateRoleMappingTask(),
			new CacheElasticsearchInstallation()
		};

		protected static IEnumerable<IClusterTeardownTask> NodeStoppedTasks { get; } = new List<IClusterTeardownTask>
		{
			new CleanUpDirectoriesAfterNodeStopped()
		};

		protected static IEnumerable<IClusterComposeTask> AfterStartedTasks  { get; } = new List<IClusterComposeTask>
		{
			new ValidateRunningVersion(),
			new ValidateClusterStateTask(),
			new PostLicenseTask(),
			new ValidateLicenseTask(),
			new ValidatePluginsTask(),
		};
	}


	public class EphemeralClusterComposer<TConfiguration> : EphemeralClusterComposerBase
		where TConfiguration : EphemeralClusterConfiguration
	{
		public EphemeralClusterComposer(IEphemeralCluster<TConfiguration> cluster) => this.Cluster = cluster;

		private IEphemeralCluster<TConfiguration> Cluster { get; }

		private bool NodeStarted { get; set; }

		public void OnStop() => Itterate(NodeStoppedTasks, (t, c, fs) => t.Run(c, this.NodeStarted), callOnStop: false);

		public void Install() => Itterate(InstallationTasks, (t, c, fs) => t.Run(c));

		public void OnBeforeStart()
		{
			var tasks = new List<IClusterComposeTask>(BeforeStart);
			if (this.Cluster.ClusterConfiguration.AdditionalBeforeNodeStartedTasks != null)
				tasks.AddRange(this.Cluster.ClusterConfiguration.AdditionalBeforeNodeStartedTasks);

			if (this.Cluster.ClusterConfiguration.PrintYamlFilesInConfigFolder)
				tasks.Add(new PrintYamlContents());

			Itterate(tasks, (t, c, fs) => t.Run(c));

			NodeStarted = true;
		}

		public void OnAfterStart()
		{
			if (this.Cluster.ClusterConfiguration.SkipBuiltInAfterStartTasks) return;
			var tasks = new List<IClusterComposeTask>(AfterStartedTasks);
			if (this.Cluster.ClusterConfiguration.AdditionalAfterStartedTasks != null)
				tasks.AddRange(this.Cluster.ClusterConfiguration.AdditionalAfterStartedTasks);
			Itterate(tasks, (t, c, fs) => t.Run(c), false);
		}

		private readonly object _lock = new object();
		private void Itterate<T>(IEnumerable<T> collection, Action<T, IEphemeralCluster<TConfiguration>, INodeFileSystem> act, bool callOnStop = true)
		{
			lock (_lock)
			{
				var cluster = this.Cluster;
				foreach (var task in collection)
				{
					try
					{
						act(task, cluster, cluster.FileSystem);
					}
					catch (Exception)
					{
						if (callOnStop) this.OnStop();
						throw;
					}
				}
			}
		}
	}
}
