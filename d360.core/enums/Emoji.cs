using System.ComponentModel;

namespace d360.core.enums
{
    public enum Emoji
    {
        [EmojiValue(1)]
        ThumbsUp = 1,
        [EmojiValue(-1)] 
        ThumbsDown = 2
    }
}
