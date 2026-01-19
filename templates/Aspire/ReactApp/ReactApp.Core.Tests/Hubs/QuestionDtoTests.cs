using ReactApp.Core.Hubs;

namespace ReactApp.Core.Tests.Hubs;

[InheritsTests]
public class QuestionDtoTests : SerializationTestsBase<QuestionDto>
{
    protected override async Task AssertAreEqual(QuestionDto expected, QuestionDto actual)
    {
        await Assert.That(actual.RoomId).IsEqualTo(expected.RoomId);
        await Assert.That(actual.QuestionText).IsEqualTo(expected.QuestionText);
        await Assert.That(actual.AuthorName).IsEqualTo(expected.AuthorName);
        await Assert.That(actual.IsAnswered).IsEqualTo(expected.IsAnswered);
        await Assert.That(actual.IsApproved).IsEqualTo(expected.IsApproved);
        await Assert.That(actual.CreatedDate).IsEqualTo(expected.CreatedDate);
        await Assert.That(actual.LastModifiedDate).IsEqualTo(expected.LastModifiedDate);
    }
    protected override QuestionDto CreateTestDto()
    {
        return new QuestionDto
        {
            RoomId = Guid.NewGuid(),
            QuestionText = "What is the meaning of life?",
            AuthorName = "Alice",
            IsAnswered = false,
            IsApproved = true,
            CreatedDate = DateTimeOffset.UtcNow,
            LastModifiedDate = DateTimeOffset.UtcNow
        };
    }
}
