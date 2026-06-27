using System;
using System.Text;
using ShadowCardSmash.Domain;
using ShadowCardSmash.Net;
using ShadowCardSmash.Net.Serialization;
using Xunit;

namespace ShadowCardSmash.Tests.Engine;

public class NetMessageJsonTests
{
    [Fact]
    public void HandshakeRequest_RoundTrip()
    {
        var msg = new HandshakeRequest(NetSessionConfig.ProtocolVersion, "Alice");
        AssertRoundTrip(msg, "HandshakeRequest");
        var clone = (HandshakeRequest)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(NetSessionConfig.ProtocolVersion, clone.ProtocolVersion);
        Assert.Equal("Alice", clone.ClientName);
    }

    [Fact]
    public void HandshakeAccepted_RoundTrip()
    {
        var token = Guid.NewGuid();
        var msg = new HandshakeAccepted(token, PlayerSide.Second);
        AssertRoundTrip(msg, "HandshakeAccepted");
        var clone = (HandshakeAccepted)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(token, clone.SessionToken);
        Assert.Equal(PlayerSide.Second, clone.AssignedSide);
    }

    [Fact]
    public void HandshakeRejected_RoundTrip()
    {
        var msg = new HandshakeRejected("Protocol mismatch (got 2, expected 1)");
        AssertRoundTrip(msg, "HandshakeRejected");
        var clone = (HandshakeRejected)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(msg.Reason, clone.Reason);
    }

    [Fact]
    public void Ping_RoundTrip()
    {
        var msg = new PingMessage(SenderTimeMs: 1234567890L);
        AssertRoundTrip(msg, "Ping");
        var clone = (PingMessage)NetMessageJson.Deserialize(NetMessageJson.Serialize(msg));
        Assert.Equal(1234567890L, clone.SenderTimeMs);
    }

    [Fact]
    public void BytesRoundTrip_UTF8()
    {
        var msg = new HandshakeRequest(NetSessionConfig.ProtocolVersion, "测试中文名");
        var bytes = NetMessageJson.SerializeToBytes(msg);
        var clone = (HandshakeRequest)NetMessageJson.DeserializeFromBytes(bytes);
        Assert.Equal("测试中文名", clone.ClientName);
    }

    [Fact]
    public void Discriminator_DispatchesCorrectType()
    {
        var accept = new HandshakeAccepted(Guid.Empty, PlayerSide.First);
        var json = NetMessageJson.Serialize(accept);
        Assert.Contains("\"$type\":\"HandshakeAccepted\"", json);
        var back = NetMessageJson.Deserialize(json);
        Assert.IsType<HandshakeAccepted>(back);
    }

    private static void AssertRoundTrip(NetMessage msg, string expectedDiscriminator)
    {
        var json1 = NetMessageJson.Serialize(msg);
        Assert.Contains($"\"$type\":\"{expectedDiscriminator}\"", json1);
        var clone = NetMessageJson.Deserialize(json1);
        Assert.Equal(msg.GetType(), clone.GetType());
        Assert.Equal(json1, NetMessageJson.Serialize(clone));
    }
}
