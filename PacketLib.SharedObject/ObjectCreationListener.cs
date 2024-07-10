namespace PacketLib.SharedObject;

public class ObjectCreationListener
{
    public event EventHandler<object> onCreate;

    public void Call(object obj)
    {
        onCreate?.Invoke(this, obj);
    }
}