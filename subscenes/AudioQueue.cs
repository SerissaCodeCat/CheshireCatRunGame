// based on : https://www.youtube.com/watch?v=bdsHf08QmZ4

using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

[Tool]
public partial class AudioQueue : Node
{
	private int _nextToPlay = 0;
	private List<AudioStreamPlayer2D> _audioStreamPlayers;
	[Export]
	public int maxNumberOfConcurrentSounds { get; set; } = 1;
	public override void _Ready()
	{
		if(GetChildCount() == 0)
		{
			GD.Print("no AudioStreamPlayer2D pressent.");
			return;
		}
		var child = GetChild(0);
		if(child is AudioStreamPlayer2D audioStream)
		{
			_audioStreamPlayers.Add(audioStream); 
			for(int i = 0; i < maxNumberOfConcurrentSounds; i++)
			{
				AudioStreamPlayer2D duplicate = audioStream.Duplicate() as AudioStreamPlayer2D;
				AddChild(duplicate);
				_audioStreamPlayers.Add(duplicate); 
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override string[] _GetConfigurationWarnings()
	{
		if(GetChildCount() == 0)
		{
			return new string[]{"no Children found, expected 1 AudiostreamPlayer2D"};
		}
		if(GetChild(0) is not AudioStreamPlayer2D)
		{
			return new string[]{"first child must be a AudioStreamPlayer2D"};
		}
		return base._GetConfigurationWarnings();
	}

	public void play()
	{
		if(!_audioStreamPlayers[_nextToPlay].Playing)
		{
			_audioStreamPlayers[_nextToPlay].Play();
		}
		_nextToPlay++;
		if(_nextToPlay == maxNumberOfConcurrentSounds)
		{
			_nextToPlay = 0;
		}
	}
}
