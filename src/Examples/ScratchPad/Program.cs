﻿using System;
using System.Security.Cryptography.X509Certificates;
using Elastic.Managed.Configuration;
using Elastic.Managed.Ephemeral;
using Elastic.Managed.Ephemeral.Plugins;
using Elasticsearch.Net;
using Nest;
using static Elastic.Managed.Ephemeral.ClusterFeatures;

namespace ScratchPad
{
	public static class Program
	{
		public static int Main()
		{
			ElasticsearchVersion version = "6.3.0";

			var plugins = new ElasticsearchPlugins(ElasticsearchPlugin.IngestGeoIp);
			var config = new EphemeralClusterConfiguration(version, plugins, 1)
			{
				ShowElasticsearchOutputAfterStarted = false,
				PrintYamlFilesInConfigFolder = false,
				CacheEsHomeInstallation = true,
				TrialMode = XPackTrialMode.Trial,
				NoCleanupAfterNodeStopped = false,
			};

			using (var cluster = new EphemeralCluster(config))
			{
				cluster.Start();

				var nodes = cluster.NodesUris();
				var connectionPool = new StaticConnectionPool(nodes);
				var settings = new ConnectionSettings(connectionPool).EnableDebugMode();
				if (config.EnableSecurity)
					settings = settings.BasicAuthentication(ClusterAuthentication.Admin.Username, ClusterAuthentication.Admin.Password);
				if (config.EnableSsl)
					settings = settings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

				var client = new ElasticClient(settings);

				Console.Write(client.XPackInfo().DebugInformation);
				Console.WriteLine("Press any key to exit");
				Console.ReadKey();
				Console.WriteLine("Exitting..");
			}
			Console.WriteLine("Done!");
			return 0;
		}
	}
}
