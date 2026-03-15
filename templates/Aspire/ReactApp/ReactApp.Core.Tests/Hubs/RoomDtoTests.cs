using ReactApp.Core.Hubs;

namespace ReactApp.Core.Tests.Hubs;

[InheritsTests]
public class RoomDtoTests : SerializationTestsBase<RoomDto>
{
    protected override async Task AssertAreEqual(RoomDto expected, RoomDto actual)
    {
        await Assert.That(actual.Id).IsEqualTo(expected.Id);
        await Assert.That(actual.FriendlyName).IsEqualTo(expected.FriendlyName);
        await Assert.That(actual.CreatedByUserId).IsEqualTo(expected.CreatedByUserId);
        await Assert.That(actual.CreatedDate).IsEqualTo(expected.CreatedDate);
        await Assert.That(actual.CurrentQuestionId).IsEqualTo(expected.CurrentQuestionId);
    }

    protected override RoomDto CreateTestDto()
    {
        return new RoomDto
        {
            Id = Guid.NewGuid(),
            FriendlyName = "Room Name",
            CreatedByUserId = "user456",
            CreatedDate = DateTimeOffset.UtcNow,
            CurrentQuestionId = Guid.NewGuid()
        };
    }
}
