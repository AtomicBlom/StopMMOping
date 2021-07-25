﻿namespace BinaryVibrance.MonoGame.Input.Binding.Button
{
	internal interface IButtonBinding : IBinding
	{
		bool Pressed { get; }
		bool Held { get; }
		bool Released { get; }
	}
}