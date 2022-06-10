namespace Wiimote.Data;

public class RegisterReadData
{
    public RegisterReadData(int offset, int size, ReadResponder responder)
    {
        Offset = offset;
        Size = size;
        Buffer = new byte[size];
        ExpectedOffset = offset;
        _responder = responder;
    }

    public int ExpectedOffset { get; private set; }

    public byte[] Buffer { get; }

    public int Offset { get;  }

    public int Size { get; }

    private readonly ReadResponder _responder;

    public bool AppendData(byte[] data)
    {
        int start = ExpectedOffset - Offset;
        int end = start + data.Length;

        if (end > Buffer.Length)
            return false;

        for (int x = start; x < end; x++)
        {
            Buffer[x] = data[x - start];
        }

        ExpectedOffset += data.Length;

        if (ExpectedOffset >= Offset + Size)
            _responder(Buffer);

        return true;
    }
    
}
