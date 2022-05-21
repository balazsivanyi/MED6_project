using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab.MultiplayerModels;
using TMPro.Examples;
using UnityEngine;
using Random = System.Random;

namespace Custom_Buttons.Did_Stuff_Buttons.Buttons {
    public class EmojiToggle : AbstractDidStuffButton {
        [SerializeField] private List<Emoji> emojis = new List<Emoji>();
        [SerializeField] private List<ParticleSystem> emojiParticles = new List<ParticleSystem>();
        private void Start()
        {

            foreach (var p in emojiParticles)
            {
                // set up the emission to generate particles
                var em = p.emission;
                em.enabled = true;
                em.rateOverTime = 0;

                em.SetBursts(
                    new[]
                    {
                        new ParticleSystem.Burst(0.0f, 5),
                    });
            }
            
        }

        
        protected override void ChangeToActiveState() {
            base.ChangeToActiveState();
            foreach (var emoji in emojis) emoji.Enabled = true;
        }

        protected override void ChangeToInactiveState() {
            base.ChangeToInactiveState();
            foreach (var emoji in emojis) emoji.Enabled = false;
        }

        public void SpawnEmoji(int index)
        {
            DeactivateButton();  
            emojiParticles[index].Play();
        }

    }
}