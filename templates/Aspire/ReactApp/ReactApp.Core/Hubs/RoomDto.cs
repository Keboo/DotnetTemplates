using ReactApp.Data;

namespace ReactApp.Core.Hubs;

public class RoomDto()
{
    internal RoomDto(Room room) : this()
    {
        Id = room.Id;
        FriendlyName = room.FriendlyName;
        CreatedByUserId = room.CreatedByUserId;
        CreatedDate = room.CreatedDate;
        CurrentQuestionId = room.CurrentQuestionId;
    }

    public Guid Id { get; set; }
    public string FriendlyName { get; set; } = "";

    public string CreatedByUserId { get; set; } = "";

    public DateTimeOffset CreatedDate { get; set; }

    public Guid? CurrentQuestionId { get; set; }

    public static explicit operator RoomDto?(Room? room) => room is null ? null : new(room);
}
