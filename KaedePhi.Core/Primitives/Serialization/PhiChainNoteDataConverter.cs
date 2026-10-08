using System;
using Newtonsoft.Json;
using REDox.Serialization;

namespace KaedePhi.Core.Primitives.Serialization
{
    internal sealed class PhiChainNoteDataConverter<TNote, TBeat> : DataConverter<TNote>
        where TNote : class
        where TBeat : struct
    {
        private readonly Func<TNote> _createNote;
        private readonly Func<TNote, int> _getType;
        private readonly Action<TNote, int> _setType;
        private readonly Func<TNote, bool> _getAbove;
        private readonly Action<TNote, bool> _setAbove;
        private readonly Func<TNote, TBeat> _getBeat;
        private readonly Action<TNote, TBeat> _setBeat;
        private readonly Func<TNote, TBeat> _getHoldBeat;
        private readonly Action<TNote, TBeat> _setHoldBeat;
        private readonly Func<TNote, float> _getX;
        private readonly Action<TNote, float> _setX;
        private readonly Func<TNote, float> _getSpeed;
        private readonly Action<TNote, float> _setSpeed;
        private readonly Func<TBeat, bool> _isPositiveBeat;
        private readonly Func<TBeat> _createZeroBeat;

        internal PhiChainNoteDataConverter(
            Func<TNote> createNote,
            Func<TNote, int> getType,
            Action<TNote, int> setType,
            Func<TNote, bool> getAbove,
            Action<TNote, bool> setAbove,
            Func<TNote, TBeat> getBeat,
            Action<TNote, TBeat> setBeat,
            Func<TNote, TBeat> getHoldBeat,
            Action<TNote, TBeat> setHoldBeat,
            Func<TNote, float> getX,
            Action<TNote, float> setX,
            Func<TNote, float> getSpeed,
            Action<TNote, float> setSpeed,
            Func<TBeat, bool> isPositiveBeat,
            Func<TBeat> createZeroBeat
        )
        {
            _createNote = createNote;
            _getType = getType;
            _setType = setType;
            _getAbove = getAbove;
            _setAbove = setAbove;
            _getBeat = getBeat;
            _setBeat = setBeat;
            _getHoldBeat = getHoldBeat;
            _setHoldBeat = setHoldBeat;
            _getX = getX;
            _setX = setX;
            _getSpeed = getSpeed;
            _setSpeed = setSpeed;
            _isPositiveBeat = isPositiveBeat;
            _createZeroBeat = createZeroBeat;
        }

        public override TNote? Read(in DataReader reader, uint tokenId, TNote? existingValue)
        {
            var note = existingValue ?? _createNote();
            _setAbove(note, false);
            _setBeat(note, _createZeroBeat());
            _setX(note, 0f);
            _setSpeed(note, 1f);

            var type = -1;
            var hasHoldBeat = false;
            var holdBeat = default(TBeat);

            foreach (var property in reader.EnumerateMap(tokenId))
            {
                switch (reader.ReadString(property.Key))
                {
                    case "kind":
                        type = ParseKind(reader.ReadString(property.Value));
                        _setType(note, type);
                        break;
                    case "above":
                        _setAbove(note, reader.ReadBoolean(property.Value));
                        break;
                    case "beat":
                        _setBeat(note, reader.ReadValue(property.Value, default(TBeat))!);
                        break;
                    case "x":
                        _setX(note, reader.ReadSingle(property.Value));
                        break;
                    case "speed":
                        _setSpeed(note, reader.ReadSingle(property.Value));
                        break;
                    case "hold_beat":
                        if (reader.IsNullToken(property.Value))
                            throw new JsonSerializationException("Hold 音符缺少持续拍。");

                        holdBeat = reader.ReadValue(property.Value, default(TBeat))!;
                        hasHoldBeat = true;
                        break;
                }
            }

            if (type < 0)
                throw new JsonSerializationException("Note kind is required.");

            if (type == 2)
            {
                if (!hasHoldBeat)
                    throw new JsonSerializationException("Hold 音符缺少持续拍。");

                if (!_isPositiveBeat(holdBeat))
                    throw new JsonSerializationException("Hold 音符的持续拍必须大于零。");

                _setHoldBeat(note, holdBeat);
            }
            else
            {
                _setHoldBeat(note, _createZeroBeat());
            }

            return note;
        }

        public override void Write(DataWriter writer, TNote? value)
        {
            if (value is null)
                throw new JsonSerializationException("Note value cannot be null.");

            var type = _getType(value);
            var kind = ToKindString(type);
            var isHold = type == 2;

            writer.WriteStartMap(isHold ? 6 : 5);
            writer.WriteSymbol("kind");
            writer.WriteString(kind);
            writer.WriteSymbol("above");
            writer.WriteBoolean(_getAbove(value));
            writer.WriteSymbol("beat");
            writer.WriteValue(_getBeat(value));
            writer.WriteSymbol("x");
            writer.WriteSingle(_getX(value));
            writer.WriteSymbol("speed");
            writer.WriteSingle(_getSpeed(value));

            if (isHold)
            {
                writer.WriteSymbol("hold_beat");
                writer.WriteValue(_getHoldBeat(value));
            }

            writer.WriteEndMap();
        }

        private static string ToKindString(int type) =>
            type switch
            {
                0 => "tap",
                1 => "drag",
                2 => "hold",
                3 => "flick",
                _ => throw new JsonSerializationException("Unknown note type."),
            };

        private static int ParseKind(string kind) =>
            kind switch
            {
                "tap" => 0,
                "drag" => 1,
                "hold" => 2,
                "flick" => 3,
                _ => throw new JsonSerializationException("Unsupported note type: " + kind),
            };
    }
}
