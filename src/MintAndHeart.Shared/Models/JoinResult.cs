namespace MintAndHeart.Shared.Models;

// Result of joining a room
public class JoinResult
{
    // Indicates if the join was successful
    public bool IsSuccess { get; set; }

    // Error message if join failed
    public string ErrorMessage { get; set; } = "";
}
