namespace Il2CppToolkit.Runtime;

public class NullableArg
{
	internal bool HasValue;

	internal object Value;
}
public class NullableArg<T> : NullableArg where T : struct
{
	internal T TypedValue;

	public NullableArg()
	{
		HasValue = false;
		Value = null;
		TypedValue = default(T);
	}

	public NullableArg(T? value)
	{
		HasValue = value.HasValue;
		Value = (HasValue ? ((object)value.Value) : null);
		TypedValue = (HasValue ? value.Value : default(T));
	}
}
