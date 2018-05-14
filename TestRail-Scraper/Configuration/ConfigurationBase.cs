using System;
using System.Collections.Generic;

public class ConfigurationBase : IConfiguration
{
	private readonly List<Type> _pipeline = new List<Type>();
	private readonly List<Type> _plugins = new List<Type>();

	public Type[] ListMiddlewareTypes()
	{
		return _pipeline.ToArray();
	}

	public Type[] ListPluginTypes()
	{
		return _plugins.ToArray();
	}
}