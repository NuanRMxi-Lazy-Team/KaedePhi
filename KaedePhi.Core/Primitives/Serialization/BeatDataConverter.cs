using System;
using REDox.Serialization;

namespace KaedePhi.Core.Primitives.Serialization
{
    internal sealed class BeatDataConverter<TBeat> : DataConverter<TBeat>
        where TBeat : struct
    {
        private readonly Func<int[], TBeat> _create;
        private readonly Func<TBeat, int, int> _getComponent;

        internal BeatDataConverter(
            Func<int[], TBeat> create,
            Func<TBeat, int, int> getComponent
        )
        {
            _create = create;
            _getComponent = getComponent;
        }

        public override TBeat Read(in DataReader reader, uint tokenId, TBeat existingValue)
        {
            if (reader.IsNullToken(tokenId))
                return default;

            var element = reader.ReadElement(tokenId);
            var values = new int[element.GetArrayLength()];
            for (var i = 0; i < values.Length; i++)
                values[i] = element[i].GetInt32();

            return _create(values);
        }

        public override void Write(DataWriter writer, TBeat value)
        {
            writer.WriteStartArray(3);
            writer.WriteInt32(_getComponent(value, 0));
            writer.WriteInt32(_getComponent(value, 1));
            writer.WriteInt32(_getComponent(value, 2));
            writer.WriteEndArray();
        }
    }
}
