using System.Text.Json;

namespace ReactApp.Core.Tests.Hubs;

public abstract class SerializationTestsBase<TDto>
    where TDto : class, new()
{
    protected abstract TDto CreateTestDto();

    protected abstract Task AssertAreEqual(TDto expected, TDto actual);

    [Test]
    public async Task CanSerializeToJson()
    {
        TDto dto = CreateTestDto();

        var json = JsonSerializer.Serialize(dto);

        await Assert.That(json).IsNotNull();
        await Assert.That(json).IsNotEmpty();
    }

    [Test]
    public async Task CanDeserializeFromJson()
    {
        TDto dto = CreateTestDto();
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TDto>(json);

        await Assert.That(deserializedDto).IsNotNull();
        await AssertAreEqual(dto, deserializedDto!);
    }
}
