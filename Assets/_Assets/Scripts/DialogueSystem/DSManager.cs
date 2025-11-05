using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class DSManager : MonoBehaviour
{
    public AudioSource audioSource;
    public DSUIManager manager;

    public string currentText;
    private string textToUI;
    private int i;

    [SerializeField] float currentTextSpeed;
    [SerializeField] float idleTextSpeed;
    [SerializeField] float punctuationTextSpeed;
    [SerializeField] float skipTextSpeedValue;
    [SerializeField] float frequency;
    [SerializeField] float messageWaitTime = 1;

    private static string HTML_ALPHA = "<color=#00000000>";

    private bool canContinue = true;
    public bool dsStarted = false;
    private bool isDialogueSkip;
    
    public Character currentSpeaker;
    public string n;

    public void SayText(string Text)
    {
        if(currentSpeaker == null && canContinue) { return; }
        currentText = Text;
        StartCoroutine(TextSpeech());
        manager.OpenDialogueBox(Text);
    }

    private IEnumerator TextSpeech()
    {
        print("Text Speech: " + currentText);
        i = 0;
        textToUI = "";
        canContinue = false;
        dsStarted = true;

        foreach (char c in currentText.ToCharArray())
        {
            i++;
            textToUI = currentText.Insert(i, HTML_ALPHA);
            PlaySound(c);
            if (manager != null)
                manager.SetCurrentText(textToUI);
            yield return new WaitForSeconds(currentTextSpeed);
        }

        yield return new WaitForSeconds(messageWaitTime);
        
        canContinue = true;
        i = 0;
        textToUI = "";
        dsStarted= false;

        manager.CloseDialogueBox();
        StopCoroutine("TextSpeech");
    }

    private void PlaySound(char sign)
    {
        if (char.IsPunctuation(sign))
        {
            audioSource.PlayOneShot(currentSpeaker.punctuationVoice);
            if ("!?.,".Contains(sign))
            {
                ChangeTextSpeed(punctuationTextSpeed);
            }
        }
        else if (char.IsLetter(sign))
        {
            if (i % frequency == 0)
            {
                audioSource.PlayOneShot(currentSpeaker.vowelVoice[0]);
                ChangeTextSpeed(idleTextSpeed);
            }
        }
        else
        {
            ChangeTextSpeed(idleTextSpeed);
        }
    }
    private void ChangeTextSpeed(float speed)
    {
        currentTextSpeed = isDialogueSkip ? speed / skipTextSpeedValue : speed;
    }
}
