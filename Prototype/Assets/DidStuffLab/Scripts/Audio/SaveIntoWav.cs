using System;
using System.Collections;
using System.IO;
using DidStuffLab.Scripts.Managers;
using UnityEngine;

namespace DidStuffLab.Scripts.Audio
{
    public class SaveIntoWav : MonoBehaviour
    {

        public const string DEFAULT_FILENAME = "DuoRhythmo";
        public const string FILE_EXTENSION = ".wav";

        public bool IsRecording => recOutput;

        private int bufferSize;
        private int numBuffers;
        private int outputRate;
        private int headerSize = 44; //default for uncompressed wav
        private String fileName;
        private bool recOutput = false;
        private AudioClip newClip;
        private FileStream fileStream;
        private AudioClip[] audioClips;
        //private AudioSource[] audioSources;
        public int currentSlot;
        float[] _tempDataSource;
        private int maxRecordTime = 60;

        void Awake()
        {
            outputRate = AudioSettings.outputSampleRate;

        }

        void Start()
        {
            AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
            /*audioSources = new AudioSource[3];
        audioSources[0] = GameObject.FindWithTag("RecSlot1").GetComponent<AudioSource>();
        audioSources[1] = GameObject.FindWithTag("RecSlot2").GetComponent<AudioSource>();
        audioSources[2] = GameObject.FindWithTag("RecSlot3").GetComponent<AudioSource>();*/
        }

        public void StartRecording()
        {
            var filename = "DuoRhythmo_" + MasterManager.Instance.currentDrumKitName + DateTime.Now.ToString("_MM-dd-yyyy_HH-mm");
            fileName = filename + FILE_EXTENSION;


            if (!recOutput)
            {
                StartWriting(fileName);
                recOutput = true;
                StartCoroutine(LimitRecording());
            }
            else
            {
                Debug.LogError("Recording is in progress already");
            }
        }

        public void StopRecording()
        {
            recOutput = false;
            WriteHeader();
            InGameMenuManager.Instance.SpawnInfoToast("Saved to: " + Application.dataPath, 0.1f);
        }


        private void StartWriting(String n)
        {
            fileStream = new FileStream( Path.Combine(Application.dataPath,n), FileMode.Create);

            var emptyByte = new byte();
            for (var i = 0; i < headerSize; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (recOutput)
            {
                ConvertAndWrite(data); //audio data is interlaced
            }
        }

        private void ConvertAndWrite(float[] dataSource)
        {

            var intData = new Int16[dataSource.Length];
            //converting in 2 steps : float[] to Int16[], //then Int16[] to Byte[]

            var bytesData = new Byte[dataSource.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            var rescaleFactor = 32767; //to convert float to Int16

            for (var i = 0; i < dataSource.Length; i++)
            {
                intData[i] = (Int16) (dataSource[i] * rescaleFactor);
                var byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);

            _tempDataSource = new float[dataSource.Length];
            _tempDataSource = dataSource;


        }

        private void WriteHeader()
        {

            fileStream.Seek(0, SeekOrigin.Begin);

            var riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            var chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            var wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            var fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            var subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            UInt16 two = 2;
            UInt16 one = 1;

            var audioFormat = BitConverter.GetBytes(one);
            fileStream.Write(audioFormat, 0, 2);

            var numChannels = BitConverter.GetBytes(two);
            fileStream.Write(numChannels, 0, 2);

            var sampleRate = BitConverter.GetBytes(outputRate);
            fileStream.Write(sampleRate, 0, 4);

            var byteRate = BitConverter.GetBytes(outputRate * 4);

            fileStream.Write(byteRate, 0, 4);

            UInt16 four = 4;
            var blockAlign = BitConverter.GetBytes(four);
            fileStream.Write(blockAlign, 0, 2);

            UInt16 sixteen = 16;
            var bitsPerSample = BitConverter.GetBytes(sixteen);
            fileStream.Write(bitsPerSample, 0, 2);

            var dataString = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(dataString, 0, 4);

            var subChunk2 = BitConverter.GetBytes(fileStream.Length - headerSize);
            fileStream.Write(subChunk2, 0, 4);

            fileStream.Close();

        }

        IEnumerator LimitRecording()
        {
            var counter = 0;
            while (IsRecording)
            {
                yield return new WaitForSeconds(1.0f);
                counter++;
                if (counter > maxRecordTime) MasterManager.Instance.Record(false);
            }
        }


    }
}

