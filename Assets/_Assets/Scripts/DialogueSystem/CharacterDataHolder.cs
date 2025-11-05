using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CharacterDataBase
{
    public static List<CharacterDataHolder> characterDatas = new List<CharacterDataHolder>();
}

[CreateAssetMenu(fileName = "CharacterDataBase")]
public class CharacterDataHolder : ScriptableObject
{
    public List<Character> characters = new List<Character>();
    public List<string> charactersNames;

    public Character FindCharacterByName(string name)
    {
        int index = characters.FindIndex(x => x.name == name);

        if (index != -1)
        {
            return characters[index];
        }

        return null;
    }
    private void OnValidate()
    {
        charactersNames = new List<string>();
        foreach (Character character in characters)
        {
            charactersNames.Add(character.name);
        }
    }
    private void OnEnable()
    {
        Debug.Log("Add");
        if (!CharacterDataBase.characterDatas.Contains(this))
            CharacterDataBase.characterDatas.Add(this);
    }
    public string[] GetCharactersEmotions(Character character)
    {
        int index = characters.FindIndex(x => character.name == x.name);
        if (index == -1) return null;
        return character.data.Keys.ToArray();
    }
    public string[] GetCharactersEmotions(int index)
    {
        return GetCharactersEmotions(characters[index]);
    }
}
[Serializable]
public class Character
{
    public string name;
    public int id;

    public AudioClip[] vowelVoice;
    public AudioClip punctuationVoice;

    [SerializedDictionary("EmotionName", "Sprites/Animations")]
    public SerializedDictionary<string, Sprite> data;

}