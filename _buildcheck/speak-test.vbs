Dim voice
Dim stream
Set voice = CreateObject("SAPI.SpVoice")
Set stream = CreateObject("SAPI.SpFileStream")
stream.Format.Type = 22
stream.Open WScript.Arguments(0), 3, False
Set voice.AudioOutputStream = stream
voice.Voice = voice.GetVoices.Item(1)
voice.Speak WScript.Arguments(1)
stream.Close
