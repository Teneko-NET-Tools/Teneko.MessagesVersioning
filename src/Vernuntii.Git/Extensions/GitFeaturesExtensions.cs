﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Vernuntii.Extensions.Configurers;
using Vernuntii.Git;
using Vernuntii.MessagesProviders;

namespace Vernuntii.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IGitFeatures"/>.
    /// </summary>
    public static class GitFeaturesExtensions
    {
        /// <summary>
        /// Adds a <see cref="IRepository"/> via <see cref="IRepositoryFactory"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="configureOptions"></param>
        public static IGitFeatures AddRepository(this IGitFeatures options, Action<RepositoryOptions>? configureOptions = null)
        {
            var services = options.Services;

            if (configureOptions != null) {
                services.AddOptions<RepositoryOptions>().Configure(configureOptions);
            }

            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<RepositoryOptions>>().Value);
            services.TryAddSingleton<IRepository, Repository>();
            return options;
        }

        /// <summary>
        /// Configures the since-commit.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="configureSinceCommit"></param>
        public static IGitFeatures ConfigureSinceCommit(this IGitFeatures extensions, Action<ISinceCommitConfigurer> configureSinceCommit)
        {
            var services = extensions.Services;
            SinceCommitConfigurer? configurer = null;

            Func<IServiceProvider, SinceCommitConfigurer> configurerProvider = sp => {
                if (configurer != null) {
                    return configurer;
                }

                configurer = ActivatorUtilities.CreateInstance<SinceCommitConfigurer>(sp);
                configureSinceCommit(configurer);
                return configurer;
            };

            services.AddSingleton<IConfigureOptions<CommitVersionFindingOptions>>(sp => configurerProvider(sp));
            services.AddSingleton<IConfigureOptions<GitCommitMessagesProviderOptions>>(sp => configurerProvider(sp));
            return extensions;
        }

        /// <summary>
        /// Configures the branch name.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="configureBranchName"></param>
        public static IGitFeatures ConfigureBranchName(this IGitFeatures extensions, Action<IBranchNameConfigurer> configureBranchName)
        {
            var services = extensions.Services;
            BranchNameConfigurer? configurer = null;

            Func<IServiceProvider, BranchNameConfigurer> configurerProvider = sp => {
                if (configurer != null) {
                    return configurer;
                }

                configurer = ActivatorUtilities.CreateInstance<BranchNameConfigurer>(sp);
                configureBranchName(configurer);
                return configurer;
            };

            services.AddSingleton<IConfigureOptions<CommitVersionFindingOptions>>(sp => configurerProvider(sp));
            services.AddSingleton<IConfigureOptions<GitCommitMessagesProviderOptions>>(sp => configurerProvider(sp));
            return extensions;
        }

        /// <summary>
        /// Configures the pre-release.
        /// </summary>
        /// <param name="extensions"></param>
        /// <param name="configurePreRelease"></param>
        public static IGitFeatures ConfigurePreRelease(this IGitFeatures extensions, Action<IPreReleaseConfigurer> configurePreRelease)
        {
            var services = extensions.Services;
            PreReleaseConfigurer? configurer = null;


            Func<IServiceProvider, PreReleaseConfigurer> configurerProvider = sp => {
                if (configurer != null) {
                    return configurer;
                }

                configurer = ActivatorUtilities.CreateInstance<PreReleaseConfigurer>(sp);
                configurePreRelease(configurer);
                return configurer;
            };

            services.AddSingleton<IConfigureOptions<CommitVersionFindingOptions>>(sp => configurerProvider(sp));
            services.AddSingleton<IConfigureOptions<SemanticVersionCalculationOptions>>(sp => configurerProvider(sp));
            return extensions;
        }

        /// <summary>
        /// Uses <see cref="GitCommitMessagesProvider"/> as <see cref="IMessagesProvider"/> and
        /// tries to register <see cref="GitCommitMessagesProviderOptions"/> from <see cref="IOptions{TOptions}"/>
        /// where T is <see cref="GitCommitMessagesProviderOptions"/>.
        /// </summary>
        /// <param name="features"></param>
        public static IGitFeatures UseCommitMessagesProvider(this IGitFeatures features)
        {
            features.Services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<GitCommitMessagesProviderOptions>>().Value);

            features.Services.ConfigureVernuntii(features => features
                .AddSemanticVersionCalculation(features => features
                    .UseMessagesProvider(sp => {
                        var repository = sp.GetRequiredService<IRepository>();
                        return ActivatorUtilities.CreateInstance<GitCommitMessagesProvider>(sp, repository);
                    })));

            return features;
        }

        /// <summary>
        /// Adds a version cache
        /// </summary>
        /// <param name="options"></param>
        public static IGitFeatures AddCommitVersionFindingCache(this IGitFeatures options)
        {
            var services = options.Services;

            services.TryAddSingleton<ICommitVersionFinder>(sp => {
                var repository = sp.GetRequiredService<IRepository>();
                return ActivatorUtilities.CreateInstance<CommitVersionFinder>(sp, repository, repository);
            });

            services.TryAddSingleton(sp => {
                var commitVersionFinder = sp.GetRequiredService<ICommitVersionFinder>();
                var findingOptions = sp.GetRequiredService<IOptions<CommitVersionFindingOptions>>().Value;
                var commitVersion = commitVersionFinder.FindCommitVersion(findingOptions);
                bool commitVersionCoreAlreadyReleased;

                if (commitVersion != null) {
                    commitVersionCoreAlreadyReleased = commitVersionFinder.IsVersionCoreReleased(commitVersion);
                } else {
                    commitVersionCoreAlreadyReleased = false;
                }

                return new CommitVersionFindingCache() {
                    CommitVersion = commitVersion,
                    CommitVersionCoreAlreadyReleased = commitVersionCoreAlreadyReleased
                };
            });

            return options;
        }

        /// <summary>
        /// Sets <see cref="SemanticVersionCalculationOptions.StartVersion"/>
        /// to <see cref="CommitVersionFindingCache.CommitVersion"/> if the
        /// latest version has been found and is present in
        /// <see cref="CommitVersionFindingCache"/>.
        /// </summary>
        /// <param name="features"></param>
        public static IGitFeatures UseLatestCommitVersion(this IGitFeatures features)
        {
            var services = features.Services;
            features.AddCommitVersionFindingCache();

            services.AddOptions<SemanticVersionCalculationOptions>()
                .Configure<IServiceProvider>((options, sp) => {
                    var findingCache = sp.GetService<CommitVersionFindingCache>();

                    if (findingCache?.CommitVersion != null) {
                        options.StartVersion = findingCache.CommitVersion;
                        options.StartVersionCoreAlreadyReleased = findingCache.CommitVersionCoreAlreadyReleased;
                    }
                });

            return features;
        }
    }
}
