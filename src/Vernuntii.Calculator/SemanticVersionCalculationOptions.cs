﻿using System.Diagnostics.CodeAnalysis;
using Vernuntii.MessagesProviders;
using Vernuntii.MessageVersioning;
using Vernuntii.SemVer;
using Vernuntii.VersionTransformers;

namespace Vernuntii
{
    /// <summary>
    /// The options class for the next version calculation.
    /// </summary>
    public sealed class SemanticVersionCalculationOptions
    {
        private static SemanticVersion GetNonNullVersionOrThrow(SemanticVersion? version) =>
            version ?? throw new ArgumentNullException(nameof(version));

        /// <summary>
        /// The start version (default is 0.1.0). All upcoming transformations are applied on this version.
        /// </summary>
        public SemanticVersion StartVersion {
            get => _startVersion;
            set => _startVersion = GetNonNullVersionOrThrow(value);
        }

        /// <summary>
        /// A boolean indicating whether the version core of <see cref="StartVersion"/> has been already released.
        /// If true a version core increment is strived.
        /// </summary>
        public bool StartVersionCoreAlreadyReleased { get; set; }

        /// <summary>
        /// The message provider.
        /// </summary>
        public IMessagesProvider? MessagesProvider { get; set; }

        /// <summary>
        /// The version core options.
        /// </summary>
        public VersionIncrementBuilderOptions VersionIncrementOptions {
            get => _versionIncrementOptions;
            set => _versionIncrementOptions = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Used on calculated version.
        /// </summary>
        public IPreReleaseTransformer? PostTransformer { get; set; }

        /// <summary>
        /// Indicates whether <see cref="PostTransformer"/> is not null and is capable to tranform.
        /// </summary>
        [MemberNotNullWhen(true, nameof(PostTransformer))]
        public bool CanPostTransform => PostTransformer is not null && !PostTransformer.DoesNotTransform;

        /// <summary>
        /// Look to the future whether the final version is about to be a release or a pre-release version.
        /// </summary>
        internal bool IsPostVersionPreRelease => !string.IsNullOrEmpty(PostTransformer?.ProspectivePreRelease ?? StartVersion.PreRelease);

        private SemanticVersion _startVersion = SemanticVersion.OneMinor.With.PreRelease("alpha");
        private VersionIncrementBuilderOptions _versionIncrementOptions = VersionIncrementBuilderOptions.Default;
    }
}
