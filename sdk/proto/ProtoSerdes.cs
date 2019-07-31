using System;
using Confluent.Kafka;
using Google.Protobuf;

namespace SDEIntegration.sdk.proto
{
  /// <summary>
  ///     protobuf serializer
  /// </summary>
  public class ProtobufSerializer<T> : ISerializer<T> where T : IMessage<T>, new()
  {
    public byte[] Serialize(T data, SerializationContext context)
        => data.ToByteArray();
  }

  /// <summary>
  ///     protobuf deserializer
  /// </summary>
  public class ProtobufDeserializer<T> : IDeserializer<T> where T : IMessage<T>, new()
  {
    private MessageParser<T> parser;

    public ProtobufDeserializer()
    {
      parser = new MessageParser<T>(() => new T());
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => parser.ParseFrom(data.ToArray());
  }
}
