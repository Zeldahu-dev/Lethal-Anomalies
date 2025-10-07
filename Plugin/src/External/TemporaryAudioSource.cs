using System.Collections;
using System.Collections.Generic;
using LethalAnomalies.Configuration;
using LethalAnomalies.External;

using UnityEngine;


namespace LethalAnomalies {

    class TemporaryAudioSource : MonoBehaviour
    {
        public AudioSource source = null!;
        public float despawnTime = 5f;
        public float randomPitchMin = 1f;
        public float randomPitchMax = 1f;
        public List<AudioClip> audioClips = new List<AudioClip>();

        public void Start()
        {
            source.pitch = Random.Range(randomPitchMin, randomPitchMax);
            if (audioClips.Count == 0)
            {
                source.Play();
                StartCoroutine(Despawn());
            }
            else
            {
                source.clip = audioClips[Random.RandomRangeInt(0, audioClips.Count)];
                source.Play();
                StartCoroutine(Despawn());
            }
        }
        private IEnumerator Despawn()
        {
            yield return new WaitForSeconds(despawnTime);
            Object.Destroy(this.gameObject);
        }
    }
}