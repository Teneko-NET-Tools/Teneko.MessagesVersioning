﻿using Microsoft.Extensions.Configuration;
using Vernuntii.HeightConventions;
using Vernuntii.MessageConventions;
using Vernuntii.VersioningPresets;

namespace Vernuntii.MessageVersioning
{
    /// <summary>
    /// Extension options for <see cref="VersionIncrementBuilder"/>
    /// </summary>
    public sealed record class VersioningPresetExtension : IEquatable<VersioningPresetExtension>
    {
        /// <summary>
        /// The "VersioningMode"-key.
        /// </summary>
        public const string VersioningModeKey = "VersioningMode";

        /// <summary>
        /// A factory for <see cref="VersioningPresetExtension"/>.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="presetManager"></param>
        /// <exception cref="ArgumentException"></exception>
        public static Func<VersioningPresetExtension> CreateFactory(IConfiguration configuration, IVersioningPresetManager presetManager)
        {
            var versioningModeSection = configuration.GetSection(VersioningModeKey);
            Func<VersioningPresetExtension> extensionFactory;

            if (!versioningModeSection.Exists() || versioningModeSection.Value != null) {
                extensionFactory = CreateFromString;

                VersioningPresetExtension CreateFromString()
                {
                    var versioningMode = configuration.GetValue(VersioningModeKey, nameof(VersioningPresetKind.Default));

                    return new VersioningPresetExtension {
                        VersioningPreset = presetManager.GetVersioningPreset(versioningMode)
                    };
                }
            } else {
                extensionFactory = CreateFromObject;

                VersioningPresetExtension CreateFromObject()
                {
                    var versioningModeObject = new VersioningModeObject();
                    versioningModeSection.Bind(versioningModeObject);

                    if (versioningModeObject.MessageConvention == null && versioningModeObject.Preset == null) {
                        throw new ArgumentException($"Field \"{nameof(versioningModeObject.MessageConvention)}\" is null." +
                            $" Either set it or specifiy the field \"{nameof(versioningModeObject.Preset)}\".");
                    } else if (versioningModeObject.IncrementMode == null && versioningModeObject.Preset == null) {
                        throw new ArgumentException($"Field \"{nameof(versioningModeObject.IncrementMode)}\" is null." +
                            $" Either set it or specifiy the field \"{nameof(versioningModeObject.Preset)}\".");
                    }

                    IVersioningPreset basePreset;

                    if (versioningModeObject.Preset != null) {
                        basePreset = presetManager.GetVersioningPreset(versioningModeObject.Preset);
                    } else {
                        basePreset = presetManager.GetVersioningPreset(VersioningPresetKind.Manual);
                    }

                    IMessageConvention? messageConvention;

                    if (versioningModeObject.MessageConvention != null) {
                        messageConvention = presetManager.GetMessageConvention(versioningModeObject.MessageConvention);
                    } else {
                        messageConvention = basePreset.MessageConvention;
                    }

                    IHeightConvention? heightConvention = basePreset.HeightConvention;
                    var versionIncrementMode = versioningModeObject.IncrementMode ?? basePreset.IncrementMode;

                    return new VersioningPresetExtension(new VersioningPreset() {
                        IncrementMode = versionIncrementMode,
                        MessageConvention = messageConvention,
                        HeightConvention = heightConvention
                    });
                }
            }

            return extensionFactory;
        }

        /// <summary>
        /// The extension name.
        /// </summary>
        internal const string ExtensionName = nameof(VersioningPresetExtension);

        /// <summary>
        /// Represents the version core options for <see cref="VersionIncrementBuilder"/>.
        /// </summary>
        public IVersioningPreset VersioningPreset {
            get => _versioningPreset ??= new VersioningPreset();
            init => _versioningPreset = value ?? throw new ArgumentNullException(nameof(value));
        }

        private IVersioningPreset? _versioningPreset;

        /// <summary>
        /// Creates an instance of this type.
        /// </summary>
        public VersioningPresetExtension() { }

        /// <summary>
        /// Creates a shallow copy of <paramref name="options"/>.
        /// </summary>
        /// <param name="options"></param>
        public VersioningPresetExtension(IVersioningPreset options) =>
            VersioningPreset = options;

        /// <inheritdoc/>
        public bool Equals(VersioningPresetExtension? other) =>
            VersioningPreset.Equals(other?.VersioningPreset);

        /// <inheritdoc/>
        public override int GetHashCode() => VersioningPreset.GetHashCode();

        internal class VersioningModeObject
        {
            public VersionIncrementMode? IncrementMode { get; set; }
            public string? Preset { get; set; }
            public string? MessageConvention { get; set; }
        }
    }
}