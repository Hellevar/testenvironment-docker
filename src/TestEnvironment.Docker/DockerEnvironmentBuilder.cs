﻿using Docker.DotNet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace TestEnvironment.Docker
{
    public class DockerEnvironmentBuilder : IDockerEnvironmentBuilder
    {
        private readonly List<IDependency> _dependencies = new List<IDependency>();
        private readonly IDictionary<string, string> _variables = new Dictionary<string, string>();
        private string _envitronmentName = Guid.NewGuid().ToString().Substring(0, 10);

        public DockerClient DockerClient { get; }

        public ILogger Logger { get; private set; } = new LoggerFactory().AddConsole().AddDebug().CreateLogger<DockerEnvironment>();

        public bool IsDockerInDocker { get; private set; } = false;

        public DockerEnvironmentBuilder()
            : this(CreateDefaultDockerClient())
        {
        }

        public DockerEnvironmentBuilder(DockerClient dockerClient)
        {
            DockerClient = dockerClient;
        }

        public IDockerEnvironmentBuilder AddDependency(IDependency dependency)
        {
            if (dependency == null) throw new ArgumentNullException(nameof(dependency));

            _dependencies.Add(dependency);

            return this;
        }

        public IDockerEnvironmentBuilder SetName(string environmentName)
        {
            if (string.IsNullOrEmpty(environmentName)) throw new ArgumentNullException(nameof(environmentName));

            _envitronmentName = environmentName;

            return this;
        }

        public IDockerEnvironmentBuilder SetVariable(params (string Name, string Value)[] variables)
        {
            if (variables == null) throw new ArgumentNullException(nameof(variables));

            foreach (var (Name, Value) in variables)
            {
                _variables.Add(Name, Value);
            }

            return this;
        }

        public IDockerEnvironmentBuilder AddContainer(string name, string imageName, string tag = "latest", (string Name, string Value)[] environmentVariables = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrEmpty(imageName)) throw new ArgumentNullException(nameof(imageName));

            var container = new Container(DockerClient, $"{_envitronmentName}-{name}", imageName, tag, environmentVariables, Logger, IsDockerInDocker);
            AddDependency(container);

            return this;
        }

        public IDockerEnvironmentBuilder UseDefaultNetwork() => throw new NotImplementedException();

        public IDockerEnvironmentBuilder DockerInDocker()
        {
            IsDockerInDocker = true;
            return this;
        }

        public IDockerEnvironmentBuilder WithLogger(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            return this;
        }

        public IDockerEnvironmentBuilder AddFromCompose(Stream composeFileStream) => throw new NotImplementedException();

        public IDockerEnvironmentBuilder AddFromDockerfile(Stream dockerfileStream) => throw new NotImplementedException();

        public DockerEnvironment Build() => new DockerEnvironment(_envitronmentName, _variables, _dependencies.ToArray(), DockerClient, Logger);

        private static DockerClient CreateDefaultDockerClient()
        {
            var dockerHostVar = Environment.GetEnvironmentVariable("DOCKER_HOST");
            var defaultDockerUrl = !string.IsNullOrEmpty(dockerHostVar)
                ? dockerHostVar
                : !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "unix:///var/run/docker.sock"
                    : "npipe://./pipe/docker_engine";

            return new DockerClientConfiguration(new Uri(defaultDockerUrl)).CreateClient();
        }
    }
}
