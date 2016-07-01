// automatically generated by the FlatBuffers compiler, do not modify

namespace Iris.Serialization.Raft
{

using System;
using FlatBuffers;

public sealed class IrisNodeFB : Table {
  public static IrisNodeFB GetRootAsIrisNodeFB(ByteBuffer _bb) { return GetRootAsIrisNodeFB(_bb, new IrisNodeFB()); }
  public static IrisNodeFB GetRootAsIrisNodeFB(ByteBuffer _bb, IrisNodeFB obj) { return (obj.__init(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public IrisNodeFB __init(int _i, ByteBuffer _bb) { bb_pos = _i; bb = _bb; return this; }

  public string MemberId { get { int o = __offset(4); return o != 0 ? __string(o + bb_pos) : null; } }
  public ArraySegment<byte>? GetMemberIdBytes() { return __vector_as_arraysegment(4); }
  public string HostName { get { int o = __offset(6); return o != 0 ? __string(o + bb_pos) : null; } }
  public ArraySegment<byte>? GetHostNameBytes() { return __vector_as_arraysegment(6); }
  public string IpAddr { get { int o = __offset(8); return o != 0 ? __string(o + bb_pos) : null; } }
  public ArraySegment<byte>? GetIpAddrBytes() { return __vector_as_arraysegment(8); }
  public int Port { get { int o = __offset(10); return o != 0 ? bb.GetInt(o + bb_pos) : (int)0; } }

  public static Offset<IrisNodeFB> CreateIrisNodeFB(FlatBufferBuilder builder,
      StringOffset MemberIdOffset = default(StringOffset),
      StringOffset HostNameOffset = default(StringOffset),
      StringOffset IpAddrOffset = default(StringOffset),
      int Port = 0) {
    builder.StartObject(4);
    IrisNodeFB.AddPort(builder, Port);
    IrisNodeFB.AddIpAddr(builder, IpAddrOffset);
    IrisNodeFB.AddHostName(builder, HostNameOffset);
    IrisNodeFB.AddMemberId(builder, MemberIdOffset);
    return IrisNodeFB.EndIrisNodeFB(builder);
  }

  public static void StartIrisNodeFB(FlatBufferBuilder builder) { builder.StartObject(4); }
  public static void AddMemberId(FlatBufferBuilder builder, StringOffset MemberIdOffset) { builder.AddOffset(0, MemberIdOffset.Value, 0); }
  public static void AddHostName(FlatBufferBuilder builder, StringOffset HostNameOffset) { builder.AddOffset(1, HostNameOffset.Value, 0); }
  public static void AddIpAddr(FlatBufferBuilder builder, StringOffset IpAddrOffset) { builder.AddOffset(2, IpAddrOffset.Value, 0); }
  public static void AddPort(FlatBufferBuilder builder, int Port) { builder.AddInt(3, Port, 0); }
  public static Offset<IrisNodeFB> EndIrisNodeFB(FlatBufferBuilder builder) {
    int o = builder.EndObject();
    return new Offset<IrisNodeFB>(o);
  }
};


}
