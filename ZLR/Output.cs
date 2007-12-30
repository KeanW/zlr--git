using System;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace ZLR.VM
{
    public delegate bool TimedInputCallback();
    public delegate void SoundFinishedCallback();
    public delegate short CharTranslator(char ch);

    /// <summary>
    /// Indicates whether a given character can be printed and/or received as input.
    /// </summary>
    /// <seealso cref="IZMachineIO.CheckUnicode"/>
    [Flags]
    public enum UnicodeCaps
    {
        /// <summary>
        /// Indicates that the character can be printed.
        /// </summary>
        CanPrint = 1,
        /// <summary>
        /// Indicates that the character can be received as input.
        /// </summary>
        CanInput = 2
    }

    /// <summary>
    /// Indicates the text style being selected in <see cref="IZMachineIO.SetTextStyle"/>.
    /// </summary>
    /// <remarks>
    /// Despite the power-of-two enum values, these styles are not bit flags, and the
    /// interface module is not expected to support setting multiple styles in a single call.
    /// </remarks>
    public enum TextStyle : ushort
    {
        /// <summary>
        /// Turns off all special text styles.
        /// </summary>
        Roman = 0,
        /// <summary>
        /// Reverses foreground and background colors.
        /// </summary>
        Reverse = 1,
        /// <summary>
        /// Boldface text.
        /// </summary>
        Bold = 2,
        /// <summary>
        /// Italic text.
        /// </summary>
        Italic = 4,
        /// <summary>
        /// Fixed pitch text.
        /// </summary>
        FixedPitch = 8,
    }

    /// <summary>
    /// Indicates the action being requested by <see cref="IZMachineIO.PlaySoundSample"/>.
    /// </summary>
    public enum SoundAction : ushort
    {
        /// <summary>
        /// Cache the sound in anticipation of playing it soon.
        /// </summary>
        Prepare = 1,
        /// <summary>
        /// Start playing the sound in the background.
        /// </summary>
        Start = 2,
        /// <summary>
        /// Stop the sound if it's currently playing.
        /// </summary>
        Stop = 3,
        /// <summary>
        /// Evict the sound from the cache because it won't be needed again soon.
        /// </summary>
        FinishWith = 4,
    }

    /// <summary>
    /// Provides an interface for Z-machine I/O features: reading and writing text;
    /// opening streams for saved games and transcripts; playing sounds; moving the cursor
    /// and splitting windows; changing the text style; and indicating the capabilities of
    /// the I/O system.
    /// </summary>
    public interface IZMachineIO
    {
        #region Input

        /// <summary>
        /// Reads a line of input from the player (or command file).
        /// </summary>
        /// <param name="time">The callback interval for timed input, in tenths of a second.
        /// If this is nonzero, <paramref name="callback"/> should be called every <paramref name="time"/>/10
        /// seconds.</param>
        /// <param name="callback">The callback function for timed input, which should be called
        /// every so often according to <paramref name="time"/>. The function can return true to cancel
        /// input immediately.</param>
        /// <param name="terminatingKeys">An array of ZSCII values of function keys which should
        /// terminate input immediately if pressed. The special value 255 means "any function key" and will
        /// appear alone.</param>
        /// <param name="terminator">Set to the ZSCII value of the key that terminated input, or 13
        /// if input was finished normally by pressing enter.</param>
        /// <returns>An empty string if input was cancelled by the timer callback, or the input string
        /// if input was finished normally by pressing enter or one of the terminating keys.</returns>
        /// <remarks>
        /// If <see cref="WritingCommandsToFile"/> is true, this method must write the command to the
        /// current command file, including any terminating character or timeout data needed to
        /// replay the input later.
        /// </remarks>
        /// <seealso cref="ReadingCommandsFromFile"/>
        /// <seealso cref="WritingCommandsToFile"/>
        string ReadLine(int time, TimedInputCallback callback, byte[] terminatingKeys, out byte terminator);
        /// <summary>
        /// Reads a single key of input from the player (or command file), without echoing it.
        /// </summary>
        /// <param name="time">The callback interval for timed input, in tenths of a second.
        /// If this is nonzero, <paramref name="callback"/> should be called every <paramref name="time"/>/10
        /// seconds.</param>
        /// <param name="callback">The callback function for timed input, which should be called
        /// every so often according to <paramref name="time"/>. The function can return true to cancel
        /// input immediately.</param>
        /// <param name="translator">A helper callback which translates printable characters into their
        /// ZSCII values, according to the currently selected translation table.</param>
        /// <returns>The ZSCII value of the key that was pressed, or 0 if input was cancelled by the
        /// timer callback.</returns>
        /// <remarks>
        /// If <see cref="WritingCommandsToFile"/> is true, this method must write the key to the
        /// current command file, including any timeout data needed to replay the input later.
        /// </remarks>
        short ReadKey(int time, TimedInputCallback callback, CharTranslator translator);
        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ReadLine"/> and <see cref="ReadKey"/>
        /// take their input from a file instead of from the player.
        /// </summary>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// The format of the command file is for the interface module to determine, but it should
        /// contain enough information to accurately record and replay timed input.
        /// </remarks>
        /// <seealso cref="ReadingCommandsFromFile"/>
        /// <seealso cref="WritingCommandsToFile"/>
        bool ReadingCommandsFromFile { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ReadLine"/> and <see cref="ReadKey"/>
        /// copy the player's input to a command file so it can be replayed later.
        /// </summary>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// The format of the command file is for the interface module to determine, but it should
        /// contain enough information to accurately record and replay timed input.
        /// </remarks>
        bool WritingCommandsToFile { get; set; }

        #endregion

        #region Output

        /// <summary>
        /// Writes a character to the screen, using the currently selected text style, cursor, and
        /// window settings.
        /// </summary>
        /// <param name="ch">The character to write.</param>
        void PutChar(char ch);
        /// <summary>
        /// Writes a string to the screen, using the currently selected text style, cursor, and
        /// window settings.
        /// </summary>
        /// <param name="str">The string to write.</param>
        void PutString(string str);
        /// <summary>
        /// Writes a series of lines to the screen, spreading down and to the right from the
        /// current cursor position, and leaving the cursor at the end of the last line.
        /// </summary>
        /// <param name="lines">The lines to write.</param>
        void PutTextRectangle(string[] lines);
        /// <summary>
        /// Gets or sets a value indicating whether text in the lower (main) window is
        /// buffered for word wrapping.
        /// </summary>
        /// <remarks>
        /// This value should be true initially when the game starts.
        /// The upper window is always buffered.
        /// </remarks>
        bool Buffering { get; set; }

        #endregion

        #region Transcripts

        /// <summary>
        /// Gets or sets a value indicating whether a transcript file is being written.
        /// </summary>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// However, if the player has already selected a transcript file during the current game,
        /// the same file should be reused instead of prompting for another name, so games can
        /// turn transcripting off and on in rapid succession.
        /// </remarks>
        bool Transcripting { get; set; }
        /// <summary>
        /// Writes a single character to the transcript file.
        /// </summary>
        /// <param name="ch">The character to write.</param>
        void PutTranscriptChar(char ch);
        /// <summary>
        /// Writes a string to the transcript file.
        /// </summary>
        /// <param name="str">The string to write.</param>
        void PutTranscriptString(string str);

        #endregion

        #region Saving the Game State

        /// <summary>
        /// Opens a stream to write the saved game file.
        /// </summary>
        /// <param name="size">The size of the game state that will be written, in bytes.</param>
        /// <returns>A writable <see cref="System.IO.Stream"/> for the save file, which the
        /// VM will close after it's done saving; or null if the user chose not to select a
        /// file or the file couldn't be opened.</returns>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// </remarks>
        Stream OpenSaveFile(int size);
        /// <summary>
        /// Opens a stream to read a previously saved game file.
        /// </summary>
        /// <returns>A readable <see cref="System.IO.Stream"/> for the save file, which the
        /// VM will close after it's done loading; or null if the user chose not to select a
        /// file or the file couldn't be opened.</returns>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// </remarks>
        Stream OpenRestoreFile();
        /// <summary>
        /// Opens a stream to read or write auxiliary game data.
        /// </summary>
        /// <param name="name">A suggested name for the auxiliary file.</param>
        /// <param name="size">The size, in bytes, of the array that will be read from or
        /// written to the auxiliary file.</param>
        /// <param name="writing">True if the stream will be used to save auxiliary data;
        /// false if it will be used to read previously saved data.</param>
        /// <returns>A <see cref="System.IO.Stream"/> for the auxiliary file, which must be
        /// readable or writable depending on the value of <paramref name="writing"/>, and
        /// which the VM will close after it's done using; or null if the user chose not to
        /// select a file or the file couldn't be opened.</returns>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// The interface module may choose to use the suggested name as-is, or prompt the user
        /// for a name and use the suggested name as a default. The suggested name should at least
        /// be visible to the user, since a game may use several auxiliary files.
        /// </remarks>
        Stream OpenAuxiliaryFile(string name, int size, bool writing);

        #endregion

        #region Visual Effects

        /// <summary>
        /// Changes the current text style.
        /// </summary>
        /// <param name="style">The style being requested.</param>
        /// <remarks>
        /// The interface module may optionally allow styles to be combined; for example, requesting
        /// italic when the bold style is already selected may result in bold italic text, or it may
        /// simply result in italic. In any case, selecting <see cref="TextStyle.Roman"/> must return
        /// to plain text.
        /// </remarks>
        /// <seealso cref="BoldAvailable"/>
        /// <seealso cref="ItalicAvailable"/>
        /// <seealso cref="FixedPitchAvailable"/>
        void SetTextStyle(TextStyle style);
        /// <summary>
        /// Changes the size of the upper window.
        /// </summary>
        /// <param name="lines">The new height of the upper window, in lines, or 0 to
        /// turn off the upper window.</param>
        void SplitWindow(short lines);
        /// <summary>
        /// Selects the upper or lower window.
        /// </summary>
        /// <param name="num">0 to select the lower window, or 1 for the upper window.</param>
        void SelectWindow(short num);
        /// <summary>
        /// Erases one or both windows.
        /// </summary>
        /// <param name="num">0 to erase the lower window, 1 to erase the upper window,
        /// -1 to erase the whole screen and turn off the upper window, or -2 to erase
        /// the whole screen but keep the windows split as they are.</param>
        /// <remarks>
        /// After erasing a window, the cursor should be returned to its upper left corner.
        /// After erasing the entire screen (-1 or -2), the lower window should be selected
        /// and the cursor returned to its upper left corner.
        /// </remarks>
        void EraseWindow(short num);
        /// <summary>
        /// Erases everything to the right of the cursor position on the current line,
        /// leaving the cursor where it is.
        /// </summary>
        void EraseLine();
        /// <summary>
        /// Moves the cursor, if the upper window is selected.
        /// </summary>
        /// <param name="x">The X coordinate of the new cursor position, counting from 1.</param>
        /// <param name="y">The Y coordinate of the new cursor position, counting from 1.</param>
        /// <remarks>
        /// The coordinate system is "screen units", the same system used by <see cref="FontHeight"/>,
        /// <see cref="FontWidth"/>, <see cref="HeightUnits"/>, and <see cref="WidthUnits"/>.
        /// </remarks>
        void MoveCursor(short x, short y);
        /// <summary>
        /// Retrieves the current cursor position, relative to the top of the currently selected
        /// window.
        /// </summary>
        /// <param name="x">Set to the cursor X coordinate, counting from 1.</param>
        /// <param name="y">Set to the cursor Y coordinate, counting from 1.</param>
        /// <remarks>
        /// The coordinate system is "screen units", the same system used by <see cref="FontHeight"/>,
        /// <see cref="FontWidth"/>, <see cref="HeightUnits"/>, and <see cref="WidthUnits"/>.
        /// </remarks>
        void GetCursorPos(out short x, out short y);
        /// <summary>
        /// Sets the current output colors.
        /// </summary>
        /// <param name="fg">The new foreground color.</param>
        /// <param name="bg">The new background color.</param>
        /// <remarks>
        /// The regular color values are: 2 (black), 3 (red), 4 (green), 5 (yellow), 6 (blue),
        /// 7 (magenta), 8 (cyan), 9 (white), 10 (light grey), 11 (medium grey), or 12 (dark grey).
        /// There are also two special color values: 0 means "no change" and 1 means "return to
        /// the default".
        /// </remarks>
        /// <seealso cref="ColorsAvailable"/>
        void SetColors(short fg, short bg);
        /// <summary>
        /// Sets the current output font.
        /// </summary>
        /// <param name="num">The new font number, or 0 to return to the previous font.</param>
        /// <returns>The previous font number, or 0 if the requested font is not available
        /// (and thus the font has not been changed).</returns>
        /// <remarks>
        /// The standard font numbers are 1 (normal font), 2 ("picture font"), 3 (character
        /// graphics font), and 4 (Courier-style fixed pitch font). However, font 2 is not
        /// expected to be supported; its definition is lost to history.
        /// </remarks>
        /// <seealso cref="GraphicsFontAvailable"/>
        short SetFont(short num);

        #endregion

        #region Sound Effects

        /// <summary>
        /// Plays, stops, or controls the cache status of a sound sample.
        /// </summary>
        /// <param name="number">The number of the sound.</param>
        /// <param name="action">The action being requested of the sound.</param>
        /// <param name="volume">The volume at which to play the sound, from 1 (quiet) to 8
        /// (loud). Values higher than 8 should be treated the same as 8, i.e., loudest.</param>
        /// <param name="repeats">The number of times the sound should be played, or 255
        /// to repeat the sound forever.</param>
        /// <param name="callback">A function to call after the sound is finished playing or
        /// repeating. This should not be called if the sound is explicitly stopped.</param>
        /// <seealso cref="SoundSamplesAvailable"/>
        /// <remarks>
        /// Sampled sounds are played in the background: this method must not wait for
        /// the sound to finish before returning.
        /// </remarks>
        void PlaySoundSample(ushort number, SoundAction action, byte volume, byte repeats,
            SoundFinishedCallback callback);
        /// <summary>
        /// Plays a beep sound.
        /// </summary>
        /// <param name="highPitch">True to play a high pitched beep, or false for a
        /// low pitched beep.</param>
        /// <remarks>
        /// Beep sounds are synchronous, so this method may wait for the beep to finish
        /// before returning.
        /// </remarks>
        void PlayBeep(bool highPitch);

        #endregion

        #region Capabilities

        /// <summary>
        /// Gets or sets a value indicating whether all text will be displayed
        /// in a fixed pitch font.
        /// </summary>
        /// <remarks>
        /// This only affects the lower window, because the upper window is always fixed pitch.
        /// </remarks>
        bool ForceFixedPitch { get; set; }

        /// <summary>
        /// Gets a value indicating whether the bold text style is available.
        /// </summary>
        /// <seealso cref="SetTextStyle"/>
        bool BoldAvailable { get; }
        /// <summary>
        /// Gets a value indicating whether the italic text style is available.
        /// </summary>
        /// <seealso cref="SetTextStyle"/>
        bool ItalicAvailable { get; }
        /// <summary>
        /// Gets a value indicating whether the fixed pitch text style is available.
        /// </summary>
        /// <seealso cref="SetTextStyle"/>
        bool FixedPitchAvailable { get; }
        /// <summary>
        /// Gets a value indicating whether the character graphics font is available.
        /// </summary>
        /// <seealso cref="SetFont"/>
        bool GraphicsFontAvailable { get; }

        /// <summary>
        /// Gets a value indicating whether timed input is available, i.e., whether
        /// the callback parameter to <see cref="ReadLine"/> and <see cref="ReadKey"/>
        /// will actually be called periodically.
        /// </summary>
        /// <seealso cref="ReadLine"/>
        /// <seealso cref="ReadKey"/>
        bool TimedInputAvailable { get; }
        /// <summary>
        /// Gets a value indicating whether sampled sound is available, i.e., whether
        /// <see cref="PlaySoundSample"/> will actually have an effect.
        /// </summary>
        /// <seealso cref="PlaySoundSample"/>
        bool SoundSamplesAvailable { get; }

        /// <summary>
        /// Gets the width of the screen in characters.
        /// </summary>
        /// <remarks>
        /// The standard "character" here is the digit "0" in the fixed pitch font.
        /// </remarks>
        byte WidthChars { get; }
        /// <summary>
        /// Gets the width of the screen in screen units.
        /// </summary>
        /// <remarks>
        /// For simplicity, it is recommended to fix the font size at 1 by 1 so that
        /// characters and screen units are the same.
        /// </remarks>
        short WidthUnits { get; }
        /// <summary>
        /// Gets the height of the screen in characters.
        /// </summary>
        /// <remarks>
        /// The standard "character" here is the digit "0" in the fixed pitch font.
        /// </remarks>
        byte HeightChars { get; }
        /// <summary>
        /// Gets the height of the screen in screen units.
        /// </summary>
        /// <remarks>
        /// For simplicity, it is recommended to fix the font size at 1 by 1 so that
        /// characters and screen units are the same.
        /// </remarks>
        short HeightUnits { get; }
        /// <summary>
        /// Gets the height of a character in screen units.
        /// </summary>
        /// <remarks>
        /// The standard "character" here is the digit "0" in the fixed pitch font.
        /// For simplicity, it is recommended to fix the font size at 1 by 1 so that
        /// characters and screen units are the same.
        /// </remarks>
        byte FontHeight { get; }
        /// <summary>
        /// Gets the width of a character in screen units.
        /// </summary>
        /// <remarks>
        /// The standard "character" here is the digit "0" in the fixed pitch font.
        /// For simplicity, it is recommended to fix the font size at 1 by 1 so that
        /// characters and screen units are the same.
        /// </remarks>
        byte FontWidth { get; }

        /// <summary>
        /// Raised when the screen size has changed.
        /// </summary>
        /// <remarks>
        /// The VM will respond by reading the new size values and writing them into
        /// the game header.
        /// </remarks>
        event EventHandler SizeChanged;

        /// <summary>
        /// Gets a value indicating whether color text is available, i.e., whether
        /// <see cref="SetColors"/> will actually have an effect.
        /// </summary>
        /// <seealso cref="SetColors"/>
        bool ColorsAvailable { get; }
        /// <summary>
        /// Gets the default foreground color.
        /// </summary>
        byte DefaultForeground { get; }
        /// <summary>
        /// Gets the default background color.
        /// </summary>
        byte DefaultBackground { get; }

        /// <summary>
        /// Determines whether a given character can be printed to the screen
        /// or received as input.
        /// </summary>
        /// <param name="ch">The character to test.</param>
        /// <returns>A <see cref="UnicodeCaps"/> value indicating whether the
        /// character can be printed or received.</returns>
        UnicodeCaps CheckUnicode(char ch);

        #endregion
    }

    partial class ZMachine
    {
        private const int DICT_WORD_SIZE = 9;

        private void PrintZSCII(short zc)
        {
            if (zc == 0)
                return;

            if (tableOutput)
            {
                List<byte> buffer = tableOutputBufferStack.Peek();
                buffer.Add((byte)zc);
            }
            else
            {
                char ch = CharFromZSCII(zc);
                if (normalOutput)
                    io.PutChar(ch);
                if (io.Transcripting)
                    io.PutTranscriptChar(ch);
            }
        }

        private void PrintUnicode(ushort uc)
        {
            if (tableOutput)
            {
                List<byte> buffer = tableOutputBufferStack.Peek();
                buffer.Add((byte)CharToZSCII((char)uc));
            }
            else
            {
                if (normalOutput)
                    io.PutChar((char)uc);
                if (io.Transcripting)
                    io.PutTranscriptChar((char)uc);
            }
        }

        private void PrintString(string str)
        {
            if (tableOutput)
            {
                List<byte> buffer = tableOutputBufferStack.Peek();
                foreach (char ch in str)
                    buffer.Add((byte)CharToZSCII(ch));
            }
            else
            {
                if (normalOutput)
                    io.PutString(str);
                if (io.Transcripting)
                    io.PutTranscriptString(str);
            }
        }

        private char CharFromZSCII(short ch)
        {
            switch (ch)
            {
                case 13:
                    return '\n';

                default:
                    if (ch >= 155 && ch < 155 + extraChars.Length)
                        return extraChars[ch - 155];
                    else
                        return (char)ch;
            }
        }

        private short CharToZSCII(char ch)
        {
            switch (ch)
            {
                case '\n':
                    return 13;

                default:
                    int idx = Array.IndexOf(extraChars, ch);
                    if (idx >= 0)
                        return (short)(155 + idx);
                    else
                        return (short)ch;
            }
        }

        private byte[] StringToZSCII(string str)
        {
            byte[] result = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
                result[i] = (byte)CharToZSCII(str[i]);
            return result;
        }

        // default alphabets (S 3.5.3)
        private static readonly char[] defaultAlphabet0 =
            { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
              'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
        private static readonly char[] defaultAlphabet1 =
            { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
              'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        private static readonly char[] defaultAlphabet2 =
            { ' ', '\n', '0', '1', '2', '3',  '4', '5', '6',  '7', '8', '9', '.',
              ',', '!',  '?', '_', '#', '\'', '"', '/', '\\', '-', ':', '(', ')' };

        // default Unicode translations (S 3.8.5.3)
        private static readonly char[] defaultExtraChars =
            { '\u00e4', '\u00f6', '\u00fc', '\u00c4', '\u00d6', '\u00dc', '\u00df', '\u00bb', '\u00ab', '\u00eb', // 155
              '\u00ef', '\u00ff', '\u00cb', '\u00cf', '\u00e1', '\u00e9', '\u00ed', '\u00f3', '\u00fa', '\u00fd', // 165
              '\u00c1', '\u00c9', '\u00cd', '\u00d3', '\u00da', '\u00dd', '\u00e0', '\u00e8', '\u00ec', '\u00f2', // 175
              '\u00f9', '\u00c0', '\u00c8', '\u00cc', '\u00d2', '\u00d9', '\u00e2', '\u00ea', '\u00ee', '\u00f4', // 185
              '\u00fb', '\u00c2', '\u00ca', '\u00ce', '\u00d4', '\u00db', '\u00e5', '\u00c5', '\u00f8', '\u00d8', // 195
              '\u00e3', '\u00f1', '\u00f5', '\u00c3', '\u00d1', '\u00d5', '\u00e6', '\u00c6', '\u00e7', '\u00c7', // 205
              '\u00fe', '\u00f0', '\u00de', '\u00d0', '\u00a3', '\u0153', '\u0152', '\u00a1', '\u00bf' };         // 215

        private string DecodeString(int address)
        {
            int dummy;
            return DecodeStringWithLen(address, out dummy);
        }

        private string DecodeStringWithLen(int address, out int len)
        {
            len = 0;

            int alphabet = 0;
            int abbrevMode = 0;
            short word;
            StringBuilder sb = new StringBuilder();

            do
            {
                word = GetWord(address);
                address += 2;
                len += 2;

                DecodeChar((word >> 10) & 0x1F, ref alphabet, ref abbrevMode, sb);
                DecodeChar((word >> 5) & 0x1F, ref alphabet, ref abbrevMode, sb);
                DecodeChar((word) & 0x1F, ref alphabet, ref abbrevMode, sb);
            } while ((word & 0x8000) == 0);

            return sb.ToString();
        }

        private void DecodeChar(int zchar, ref int alphabet, ref int abbrevMode, StringBuilder sb)
        {
            switch (abbrevMode)
            {
                case 1:
                case 2:
                case 3:
                    sb.Append(GetAbbreviation((short)(32 * (abbrevMode - 1) + zchar)));
                    abbrevMode = 0;
                    return;

                case 4:
                    abbrevMode = 5;
                    alphabet = zchar;
                    return;
                case 5:
                    abbrevMode = 0;
                    sb.Append(CharFromZSCII((short)((alphabet << 5) + zchar)));
                    alphabet = 0;
                    return;
            }

            switch (zchar)
            {
                case 0:
                    sb.Append(' ');
                    return;

                case 1:
                case 2:
                case 3:
                    abbrevMode = zchar;
                    return;

                case 4:
                    alphabet = 1;
                    return;
                case 5:
                    alphabet = 2;
                    return;
            }

            zchar -= 6;
            switch (alphabet)
            {
                case 0:
                    sb.Append(alphabet0[zchar]);
                    return;

                case 1:
                    sb.Append(alphabet1[zchar]);
                    alphabet = 0;
                    return;

                case 2:
                    if (zchar == 0)
                        abbrevMode = 4;
                    else
                        sb.Append(alphabet2[zchar]);
                    alphabet = 0;
                    return;
            }
        }

        private string GetAbbreviation(int num)
        {
            ushort address = (ushort)GetWord(abbrevTable + num * 2);
            return DecodeString(address * 2); // word address, not byte address!
        }

        private short ReadImpl(ushort buffer, ushort parse, ushort time, ushort routine)
        {
            byte max = GetByte(buffer);
            byte offset = GetByte(buffer + 1);

            byte terminator;
            string str;

            BeginExternalWait();
            try
            {
                str = io.ReadLine(time, delegate { return HandleInputTimer(routine); },
                    terminatingChars, out terminator);
            }
            finally
            {
                EndExternalWait();
            }

            byte[] chars = StringToZSCII(str);
            SetByte(buffer + 1, (byte)chars.Length);
            for (int i = 0; i < Math.Min(chars.Length, max - offset); i++)
                SetByte(buffer + 2 + offset + i, chars[i]);

            if (parse != 0)
                Tokenize(buffer, parse, 0, false);

            return terminator;
        }

        private short ReadCharImpl(ushort time, ushort routine)
        {
            BeginExternalWait();
            try
            {
                return io.ReadKey(time, delegate { return HandleInputTimer(routine); }, CharToZSCII);
            }
            finally
            {
                EndExternalWait();
            }
        }

        private bool HandleInputTimer(ushort routine)
        {
            EnterFunctionImpl((short)routine, null, 0, pc);

            JitLoop();

            short result = stack.Pop();
            return (result != 0);
        }

        private void HandleSoundFinished(ushort routine)
        {
            EnterFunctionImpl((short)routine, null, 0, pc);
            JitLoop();
        }

        private struct Token
        {
            public byte StartPos, Length;

            public Token(byte startPos, byte Length)
            {
                this.StartPos = startPos;
                this.Length = Length;
            }
        }

        private bool IsTokenSpace(byte ch)
        {
            return (ch == 9) || (ch == 32);
        }

        private List<Token> SplitTokens(byte[] buffer, ushort userDict)
        {
            List<Token> result = new List<Token>();
            byte[] seps;

            if (userDict == 0)
            {
                seps = wordSeparators;
            }
            else
            {
                byte n = GetByte(userDict);
                seps = new byte[n];
                GetBytes(userDict + 1, n, seps, 0);
            }

            int i = 0;
            do
            {
                // skip whitespace
                while (i < buffer.Length && IsTokenSpace(buffer[i]))
                    i++;

                if (i >= buffer.Length)
                    break;

                // found a separator?
                if (Array.IndexOf(seps, buffer[i]) >= 0)
                {
                    result.Add(new Token((byte)i, 1));
                    i++;
                }
                else
                {
                    byte start = (byte)i;

                    // find the end of the word
                    while (i < buffer.Length && !IsTokenSpace(buffer[i]) &&
                            Array.IndexOf(seps, buffer[i]) == -1)
                    {
                        i++;
                    }

                    // add it to the list
                    result.Add(new Token(start, (byte)(i - start)));
                }
            } while (i < buffer.Length);

            return result;
        }

        private void Tokenize(ushort buffer, ushort parse, ushort userDict, bool skipUnrecognized)
        {
            byte bufLen = GetByte(buffer + 1);
            byte max = GetByte(parse + 0);
            byte count = 0;

            byte[] myBuffer = new byte[bufLen];
            GetBytes(buffer + 2, bufLen, myBuffer, 0);
            List<Token> tokens = SplitTokens(myBuffer, userDict);

            foreach (Token tok in tokens)
            {
                ushort word = LookUpWord(userDict, myBuffer, tok.StartPos, tok.Length);
                if (word == 0 && skipUnrecognized)
                    continue;

                SetWord(parse + 2 + 4 * count, (short)word);
                SetByte(parse + 2 + 4 * count + 2, tok.Length);
                SetByte(parse + 2 + 4 * count + 3, (byte)(2 + tok.StartPos));
                count++;

                if (count == max)
                    break;
            }

            SetByte(parse + 1, count);
        }

        private ushort LookUpWord(int userDict, byte[] buffer, int pos, int length)
        {
            int dictStart;
            byte[] word;

            word = EncodeText(buffer, pos, length, DICT_WORD_SIZE);

            if (userDict != 0)
            {
                byte n = GetByte(userDict);
                dictStart = userDict + 1 + n;
            }
            else
            {
                dictStart = dictionaryTable + 1 + wordSeparators.Length;
            }

            byte entryLength = GetByte(dictStart++);

            int entries;
            if (userDict == 0)
                entries = (ushort)GetWord(dictStart);
            else
                entries = GetWord(dictStart);
            dictStart += 2;

            if (entries < 0)
            {
                // use linear search for unsorted user dictionary
                for (int i = 0; i < entries; i++)
                {
                    int addr = dictStart + i * entryLength;
                    if (CompareWords(word, addr) == 0)
                        return (ushort)addr;
                }
            }
            else
            {
                // use binary search
                int start = 0, end = entries;
                while (start < end)
                {
                    int mid = (start + end) / 2;
                    int addr = dictStart + mid * entryLength;
                    int cmp = CompareWords(word, addr);
                    if (cmp == 0)
                        return (ushort)addr;
                    else if (cmp < 0)
                        end = mid;
                    else
                        start = mid + 1;
                }
            }

            return 0;
        }

        private int CompareWords(byte[] word, int addr)
        {
            for (int i = 0; i < word.Length; i++)
            {
                int cmp = word[i] - GetByte(addr + i);
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }

        /// <summary>
        /// Encodes a section of text, optionally truncating or padding the output to a fixed size.
        /// </summary>
        /// <param name="input">The buffer containing the plain text.</param>
        /// <param name="start">The index within <paramref name="input"/> where the
        /// plain text starts.</param>
        /// <param name="length">The length of the plain text.</param>
        /// <param name="numZchars">The number of 5-bit characters that the output should be
        /// truncated or padded to, which must be a multiple of 3; or 0 to allow variable size
        /// output (padded up to a multiple of 2 bytes, if necessary).</param>
        /// <returns>The encoded text, with th.</returns>
        private byte[] EncodeText(byte[] input, int start, int length, int numZchars)
        {
            List<byte> zchars;
            if (numZchars == 0)
            {
                zchars = new List<byte>(length);
            }
            else
            {
                if (numZchars < 0 || numZchars % 3 != 0)
                    throw new ArgumentException("Output size must be a multiple of 3", "numZchars");
                zchars = new List<byte>(numZchars);
            }

            for (int i = 0; i < length; i++)
            {
                byte zc = input[start + i];
                char ch = char.ToLower(CharFromZSCII(zc));

                if (ch == ' ')
                {
                    zchars.Add(0);
                }
                else
                {
                    int alpha;
                    if ((alpha = Array.IndexOf(alphabet0, ch)) >= 0)
                    {
                        zchars.Add((byte)(alpha + 6));
                    }
                    else if ((alpha = Array.IndexOf(alphabet1, ch)) >= 0)
                    {
                        zchars.Add(4);
                        zchars.Add((byte)(alpha + 6));
                    }
                    else if ((alpha = Array.IndexOf(alphabet2, ch)) >= 0)
                    {
                        zchars.Add(5);
                        zchars.Add((byte)(alpha + 6));
                    }
                    else
                    {
                        zchars.Add(5);
                        zchars.Add(6);
                        zchars.Add((byte)(zc >> 5));
                        zchars.Add((byte)(zc & 31));
                    }
                }
            }

            int resultBytes;
            if (numZchars == 0)
            {
                // pad up to a multiple of 3
                while (zchars.Count % 3 != 0)
                    zchars.Add(5);
                resultBytes = zchars.Count * 2 / 3;
            }
            else
            {
                // pad up to the fixed size
                while (zchars.Count < numZchars)
                    zchars.Add(5);
                resultBytes = numZchars * 2 / 3;
            }

            byte[] result = new byte[resultBytes];
            int zi = 0, ri = 0;
            while (ri < resultBytes)
            {
                result[ri] = (byte)((zchars[zi] << 2) | (zchars[zi + 1] >> 3));
                result[ri + 1] = (byte)((zchars[zi + 1] << 5) | zchars[zi + 2]);
                ri += 2;
                zi += 3;
            }

            result[resultBytes - 2] |= 128;
            return result;
        }

        private void SetOutputStream(short num, ushort address)
        {
            bool enabled = true;
            if (num < 0)
            {
                num = (short)-num;
                enabled = false;
            }

            switch (num)
            {
                case 1:
                    // normal
                    normalOutput = enabled;
                    break;

                case 2:
                    // transcript
                    io.Transcripting = enabled;
                    break;

                case 3:
                    // memory (nestable up to 16 levels)
                    if (enabled)
                    {
                        if (tableOutputAddrStack.Count == 16)
                            throw new Exception("Output stream 3 nested too deeply");
                        if (address < 64 || address + 1 >= romStart)
                            throw new Exception("Output stream 3 address is out of range");

                        tableOutput = true;
                        tableOutputAddrStack.Push(address);
                        tableOutputBufferStack.Push(new List<byte>());
                    }
                    else if (tableOutput)
                    {
                        address = tableOutputAddrStack.Pop();
                        List<byte> buffer = tableOutputBufferStack.Pop();

                        int len = Math.Min(buffer.Count, romStart - address - 2);
                        SetWord(address, (short)len);
                        for (int i = 0; i < len; i++)
                            SetByte(address + 2 + i, buffer[i]);

                        if (tableOutputAddrStack.Count == 0)
                            tableOutput = false;
                    }
                    break;

                case 4:
                    // player's commands
                    io.WritingCommandsToFile = enabled;
                    break;

                default:
                    throw new Exception("Invalid output stream #" + num.ToString());
            }
        }

        private void SetInputStream(short num)
        {
            switch (num)
            {
                case 0:
                    io.ReadingCommandsFromFile = false;
                    break;

                case 1:
                    io.ReadingCommandsFromFile = true;
                    break;

                default:
                    throw new Exception("Invalid input stream #" + num.ToString());
            }
        }

        private void GetCursorPos(ushort address)
        {
            short x, y;
            io.GetCursorPos(out x, out y);
            SetWordChecked(address, y);
            SetWordChecked(address + 2, x);
        }
    }
}