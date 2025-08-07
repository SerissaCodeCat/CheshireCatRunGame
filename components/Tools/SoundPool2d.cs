using Godot;
using System.Collections.Generic;

public partial class SoundPool2d : Node2D
{
	private List<AudioQueue2D> list = new List<AudioQueue2D>();
	private RandomNumberGenerator rand = new RandomNumberGenerator();
	private int previousIndex = -1;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach(var child in GetChildren())
		{
			if(child is AudioQueue2D audioQueue)
			list.Add(audioQueue);
		}
	}

	//play a sound fron the list of potential sound in the pool, at the specified location
	public void playAtLocation(float x, float y)
	{
		int index = 0;
		if(list.Count > 1) // if there is only 1 sound in the pool, just play that sound
		{
			int repeatGuard = 0;
			do
			{
				index = rand.RandiRange(0, list.Count-1);
				repeatGuard++;
			}while(index == previousIndex && repeatGuard < 5); //if random sound == the previous sound 4 times, just play the first sound
			if(repeatGuard == 5)
			{
				index = 0;
			}
		} 
		list[index].playAtLocation(x,y);
		previousIndex = index;
	}
}
