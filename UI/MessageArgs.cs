using System;

namespace UI;

public class MessageArgs(string message) : EventArgs
{
    public string Message { get; set; } = message;
}