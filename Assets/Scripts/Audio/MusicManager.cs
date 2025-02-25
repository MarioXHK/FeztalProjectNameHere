using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

public class MusicManager : MonoBehaviour
{
    public AudioSource bass;
    public AudioSource bells;
    public AudioSource drums;
    public AudioSource lead;
	public AudioSource beep;
	public AudioSource boop;
    public AudioSource misc;
	public AudioSource extra;

    public float volumeMultiplier = 0.8f;
    public float pauseVolumeMultiplier = 0.2f;

    [Serializable]
    public struct Volumes
    {
        public float bass;
        public float bells;
        public float drums;
        public float lead;
        public float beep;
		public float boop;
		public float misc;
		public float extra;
    }

    [Serializable]
    public struct VolumesPreset
    {
        public string mapName;
        public Volumes volumes;
        public float interpolation;
    }

    [SerializeField]
    public List<VolumesPreset> volumesPresets;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }


    void Update()
    {
        UpdateVolumes(false);

        bass.ignoreListenerPause = true;
        bells.ignoreListenerPause = true;
        drums.ignoreListenerPause = true;
        lead.ignoreListenerPause = true;
        beep.ignoreListenerPause = true;
		boop.ignoreListenerPause = true;
		misc.ignoreListenerPause = true;
		extra.ignoreListenerPause = true;
    }

    void UpdateVolumes(bool immediate) {
        
        string sceneName = SceneManager.GetActiveScene().name;
        int index = volumesPresets.FindIndex(p => p.mapName == sceneName);

        // special case for finished levels
        string sceneNameFinished = SceneManager.GetActiveScene().name+"F";
        int indexFinished = volumesPresets.FindIndex(p => p.mapName == sceneNameFinished);
        if (indexFinished >= 0 && LevelManager.PuzzleFinished) {
            index = indexFinished;
        }

        if (index >= 0) {
            Volumes preset = volumesPresets[index].volumes;
            bool paused = LevelManager.IsPaused();
            float mult = (paused ? pauseVolumeMultiplier : volumeMultiplier);

            float t = immediate ? 1 : Time.unscaledDeltaTime / (paused ? 1 : volumesPresets[index].interpolation);

            bass.volume = Mathf.MoveTowards(bass.volume, preset.bass * mult, t);
            bells.volume = Mathf.MoveTowards(bells.volume, preset.bells * mult, t);
            drums.volume = Mathf.MoveTowards(drums.volume, preset.drums * mult, t);
            lead.volume = Mathf.MoveTowards(lead.volume, preset.lead * mult, t);
            beep.volume = Mathf.MoveTowards(beep.volume, preset.beep * mult, t);
			boop.volume = Mathf.MoveTowards(boop.volume, preset.boop * mult, t);
			misc.volume = Mathf.MoveTowards(misc.volume, preset.misc * mult, t);
			extra.volume = Mathf.MoveTowards(extra.volume, preset.extra * mult, t);
        }
    }

    public void Play() {
        Debug.Log("PLAY!");
        UpdateVolumes(true);
        bass.Play();
        bells.Play();
        drums.Play();
        lead.Play();
		beep.Play();
		boop.Play();
        misc.Play();
		extra.Play();
    }
}
