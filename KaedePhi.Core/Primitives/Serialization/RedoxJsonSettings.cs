using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using REDox.Serialization.Metadata;
using REDox.Serialization.NewtonsoftJson;

namespace KaedePhi.Core.Primitives.Serialization
{
    internal sealed class RedoxJsonSettings : NewtonsoftJsonSerializerSettings
    {
        internal RedoxJsonSettings(JsonSerializerSettings settings)
            : base(settings) { }

        protected override DataContract ResolveContract(Type type)
        {
            var contract = base.ResolveContract(type);

            if (type == typeof(global::KaedePhi.Core.Primitives.Beat))
            {
                return contract with
                {
                    Converter = new BeatDataConverter<global::KaedePhi.Core.Primitives.Beat>(
                        values => new global::KaedePhi.Core.Primitives.Beat(values),
                        (value, index) => value[index]
                    ),
                };
            }

            if (type == typeof(global::KaedePhi.Core.Common.Beat))
            {
                return contract with
                {
                    Converter = new BeatDataConverter<global::KaedePhi.Core.Common.Beat>(
                        values => new global::KaedePhi.Core.Common.Beat(values),
                        (value, index) => value[index]
                    ),
                };
            }

            if (type == typeof(global::KaedePhi.Core.Formats.PhiChain.v6.Model.Note))
            {
                return contract with
                {
                    Converter = new PhiChainNoteDataConverter<
                        global::KaedePhi.Core.Formats.PhiChain.v6.Model.Note,
                        global::KaedePhi.Core.Primitives.Beat
                    >(
                        () => new global::KaedePhi.Core.Formats.PhiChain.v6.Model.Note(),
                        note => (int)note.Type,
                        (note, value) =>
                            note.Type =
                                (global::KaedePhi.Core.Formats.PhiChain.v6.Model.NoteType)value,
                        note => note.Above,
                        (note, value) => note.Above = value,
                        note => note.Beat,
                        (note, value) => note.Beat = value,
                        note => note.HoldBeat,
                        (note, value) => note.HoldBeat = value,
                        note => note.X,
                        (note, value) => note.X = value,
                        note => note.Speed,
                        (note, value) => note.Speed = value,
                        beat => beat > new global::KaedePhi.Core.Primitives.Beat(0),
                        () => new global::KaedePhi.Core.Primitives.Beat(0)
                    ),
                };
            }

            if (type == typeof(global::KaedePhi.Core.PhiChain.v6.Note))
            {
                return contract with
                {
                    Converter = new PhiChainNoteDataConverter<
                        global::KaedePhi.Core.PhiChain.v6.Note,
                        global::KaedePhi.Core.Common.Beat
                    >(
                        () => new global::KaedePhi.Core.PhiChain.v6.Note(),
                        note => (int)note.Type,
                        (note, value) => note.Type = (global::KaedePhi.Core.PhiChain.v6.NoteType)value,
                        note => note.Above,
                        (note, value) => note.Above = value,
                        note => note.Beat,
                        (note, value) => note.Beat = value,
                        note => note.HoldBeat,
                        (note, value) => note.HoldBeat = value,
                        note => note.X,
                        (note, value) => note.X = value,
                        note => note.Speed,
                        (note, value) => note.Speed = value,
                        beat => beat > new global::KaedePhi.Core.Common.Beat(0),
                        () => new global::KaedePhi.Core.Common.Beat(0)
                    ),
                };
            }

            List<DataProperty>? properties = null;

            for (var i = 0; i < contract.Properties.Count; i++)
            {
                var property = contract.Properties[i];
                var converterType = property
                    .Info.GetCustomAttribute<JsonConverterAttribute>(inherit: true)
                    ?.ConverterType;

                if (
                    converterType
                        == typeof(global::KaedePhi.Core.Formats.Phigros.v3.Serialization.JsonConverter.NoteTypeConverter)
                    || converterType
                        == typeof(global::KaedePhi.Core.Phigros.v3.JsonConverter.NoteTypeConverter)
                )
                {
                    properties ??= new List<DataProperty>(contract.Properties);
                    properties[i] = property with { Converter = GetConverter(property.PropertyType) };
                }
            }

            return properties is null ? contract : contract with { Properties = properties };
        }
    }
}
