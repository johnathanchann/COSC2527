using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Statistic;

public class DrawGUI : MonoBehaviour
{
	public Sprite HeartSprite;

	public Sprite FlySprite;

	private int _iconSize = 20;
	private int _iconSeparation = 10;

	private int Health;

	private Texture2D _heartTex;

	private Texture2D _flyTex;
	private Frog frog;



	void Start()
	{
		

		_heartTex = SpriteToTexture(HeartSprite);
		_flyTex = SpriteToTexture(FlySprite);
		frog = GameObject.FindWithTag("Frog").GetComponent<Frog>();

	}
	

	void reset(){
		Frog.Health=3;
		Frog.fly_eat=0;
	}
	void OnGUI()
	{
		if (Frog.Health <= 0 || Frog.fly_eat >= 10)
		{
			if (Frog.input >= 0 && Frog.input <= 2)
			{
				if (Frog.Health <= 0)
				{
					Stats.losses[Frog.input]++;
				}
				if (Frog.fly_eat >= 10)
				{
					Stats.wins[Frog.input]++;
				}
			}
			reset();
			//include winrate test
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
		int _maxFiles = 9;

		GUI.Box(new Rect(10, 10, 30 * _maxFiles + 10, 60), "");

		for (int i = 0; i < Frog.Health; i++)
		{
			GUI.DrawTexture(new Rect(20 + (_iconSize + _iconSeparation) * i, 20, _iconSize, _iconSize), _heartTex, ScaleMode.ScaleToFit, true, 0.0f);
		}
		for (int i = 0; i < Frog.fly_eat; i++)
		{
			GUI.DrawTexture(new Rect(20 + (_iconSize + _iconSeparation) * i, 45, _iconSize, _iconSize), _flyTex, ScaleMode.ScaleToFit, true, 0.0f);
		}
		string modeText = "";
		if (Frog.human)
		{
			modeText = "human";
		}
		else if (Frog.input == 0)
		{
			modeText = "v1";
		}
		else if (Frog.input == 1)
		{
			modeText = "v2";
		}
		else if (Frog.input == 2)
		{
			modeText = "v3(behavioral tree)";
		}
		GUI.Label(new Rect(20, 110, 200, 30), modeText);

		int statIndex = Frog.input;
		GUI.Label(new Rect(20, 145, 200, 30), $"Wins: {Stats.wins[Frog.input]} Losses: {Stats.losses[Frog.input]}");



	}

	// Helper function to convert sprites to textures.
	// Follows the code from http://answers.unity3d.com/questions/651984/convert-sprite-image-to-texture.html
	private Texture2D SpriteToTexture(Sprite sprite)
	{
		if (sprite.rect.width != sprite.texture.width)
		{
			Texture2D texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
			Color[] pixels = sprite.texture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height);
			texture.SetPixels(pixels);
			texture.Apply();

			return texture;
		}
		else
		{
			return sprite.texture;
		}
	}
}
