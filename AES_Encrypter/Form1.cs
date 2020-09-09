using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;

namespace AES_Encrypter
{
    public partial class Form1 : Form
    {
        public List<Block> pTBlocks = new List<Block>();    //128-bit plaintext blocks List representing plaintext values
        public List<Block> roundKeys= new List<Block>();  //  Anew list of expansion
        public List<Block> addBlocks = new List<Block>();
        public List<Block> subBlocks = new List<Block>();
        public List<Block> shiftBlocks = new List<Block>();
        public List<Block> mixBlocks = new List<Block>();
        public List<Block> cTBlocks = new List<Block>();
        Block publicKey = new Block();
        byte[] expandedKey = new byte[44];
        int[,] sBox = new int[16, 16];        //hold entire sbox values for 16x 16 array
        int[,] invSBox = new int[16, 16];        //hold entire sbox values for 16x 16 array
        public int msgLength = 0;
        bool sBoxGenerated = false;
        bool keyGenerated = false;
        bool msgEncrypted = false;
        bool keySet = false;
        const int rijndaelPoly = 283;  //integer value for Rijnadel polynomial

        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Title = "Browse Text Files";
            openFileDialog1.DefaultExt = "txt";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Filter = "Text Files (.txt)|*.txt";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.Multiselect = false;

            GenForwardSBox();
            publicKeyBox.Text = "8171B61E1CBA9F4C52D5B11EE3BD4B59";

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Generate keys button
            GenerateKey();
            publicKeyBox.Text = publicKey.printBlock();
            keySet = true;

        }


        private void button2_Click(object sender, EventArgs e)
        {
            //Load text file into PTBlocks object
            Stream myStream = null;
            openFileDialog1 = new OpenFileDialog();
            char[] buf = new char[Block.blockSize];
            byte[] byteArray = new  byte[Block.blockSize];
            int fileIndex = 0;
            int count = 0;
            int readCount;

            ClearLists();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                Console.WriteLine(openFileDialog1.FileName);
                try
                {
                    if (openFileDialog1.OpenFile() != null)
                    {
                        myStream = openFileDialog1.OpenFile();
                        if (myStream == null)
                        {
                            MessageBox.Show("Error: myStream is null");
                        } else { 
                           
                            StreamReader sr = new StreamReader(myStream);
                            do
                            {
                                readCount = sr.ReadBlock(buf, 0, Block.blockSize);
                                if (readCount == 0) break;
                                //Console.WriteLine("Block #" +pTBlocks.Count);
                                pTBlocks.Add(new Block());
                                for (int i = 0; i < readCount; i++)
                                {
                                    pTBlocks[count].setByte((byte)buf[i], i);
                                    //Console.WriteLine("Char: " + buf[i] + ", Byte: 0x" + pTBlocks[count].getByte(i).ToString("X"));
                                }
                                fileIndex += readCount;
                                count++;
                                //Console.WriteLine("new file index = " + fileIndex);
                            }
                            while (readCount == Block.blockSize);
                            msgLength = pTBlocks.Count;
                            msgEncrypted = false;
                            //write to plainText listview1
                            Console.WriteLine("Total file length =" + fileIndex + ", Total blocks = " + pTBlocks.Count);
                            ptLabel.Text = "Plain Text from file : Total blocks = " + pTBlocks.Count;
                            listView1.Clear();    
                            for (int i =0; i < msgLength; i++)
                            {
                                listView1.Items.Add(pTBlocks[i].printBlock());
                            }

                            sr.Close();
                            sr.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk.  Original error: " + ex.Message);
                }

                initBlockList(addBlocks, msgLength);
                initBlockList(subBlocks, msgLength);
                initBlockList(shiftBlocks, msgLength);
                initBlockList(mixBlocks, msgLength);
                initBlockList(cTBlocks, msgLength);
                Console.WriteLine("All Blocks initiated.");
                Console.WriteLine("Length of mixBlocks = " + mixBlocks.Count + " MixBlocks[0] = " + mixBlocks[0].printBlock());

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            //Encode button
            if (!keySet)
            {
                string boxText = publicKeyBox.Text;
                if (boxText == "")
                {
                    MessageBox.Show("Error: public Key field  not set please either enter hex key(32 hex digits) or generate random");
                    return;
                }
                else if (boxText.Length != 32)
                {
                    MessageBox.Show("Error: public Key field  must be 32 hex digits long");
                    return;
                }
                publicKey.readBlock(boxText);
                keySet = true;

            }
            Console.WriteLine("Key is ready to begin encoding " + publicKey.printBlock());
            ExpandKeySchedule();
            //msgLength = 1;
            //byte[] testBytes = { 0x4A, 0xC3, 0x46, 0xE7, 0xD8, 0x95, 0xA6, 0x8C, 0xF2, 0x87, 0x97, 0x4D, 0x90, 0xEC, 0x6E, 0x4C };
            //Block testBlock = new Block();
            //Block addBlock = new Block();
            //int i;
            //for ( i=0; i < Block.blockSize; i++)
            //{
            //    testBlock.setByte(testBytes[i], i);
            //}
            //List<Block> testList = new List<Block>();
            //testList.Add(testBlock);
            ////Test Add Blocks inversion
            //Console.WriteLine("Add key test: " + testList[0].printBlock() );
            //Console.WriteLine("   Round key: " + roundKeys[0].printBlock());
            //addBlocks = AddRoundKey(0, testList);
            //Console.WriteLine("      Result: " + addBlocks[0].printBlock());
            //testList = AddRoundKey(0, addBlocks);
            //Console.WriteLine(" Invert test: " + testList[0].printBlock());
            ////test SubBytes inversion
            //subBlocks = SubBytes(testList);
            //Console.WriteLine("  Sub Result: " + subBlocks[0].printBlock());
            //testList = InvSubBytes(subBlocks);
            //Console.WriteLine(" invert test: " + testList[0].printBlock());
            ////test ShiftRows inversion
            //shiftBlocks = ShiftRows(testList);
            //Console.WriteLine("Shift Result: " + shiftBlocks[0].printBlock());
            //testList = InvShiftRows(shiftBlocks);
            //Console.WriteLine(" invert test: " + testList[0].printBlock());
            //// test MixColumns inversion
            //mixBlocks = MixColumns(testList);
            //testList = InvMixColumns(mixBlocks);
            encryptAES();
            //Write to Cypher Text box listView2
            listView2.Clear();
            ctLabel.Text = "Cypher Text : Total blocks = " + cTBlocks.Count;
            for (int i = 0; i < msgLength; i++)
            {
                listView2.Items.Add(cTBlocks[i].printBlock());
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Write cypher text to file
            string fileName = textBox1.Text + ".aes";

            using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                int i, j;
                for (i = 0; i < msgLength; i++)
                {
                    for (j = 0; j < Block.blockSize; j++)
                    {
                        writer.Write(cTBlocks[i].getByte(j));
                    }

                }
                MessageBox.Show("Cypher Text has been written to " + fileName);
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            //Decode button
            decryptAES();
            //write to plainText listview1
            ptLabel.Text = "Decoded from Cypher Text : Total blocks = " + pTBlocks.Count;
            listView1.Clear();
            
            for (int i = 0; i < msgLength; i++)
            {
                listView1.Items.Add(pTBlocks[i].printBlock());
            }
        }

        private void ClearLists()
        {
            pTBlocks.Clear();
            addBlocks.Clear();
            subBlocks.Clear();
            shiftBlocks.Clear();
            mixBlocks.Clear();
            cTBlocks.Clear();
        }

        private void GenerateKey()
        {
            string message = "If you change keys then you will not be able to decrypt the current encoded message. Proceed anywat?";
            string caption = "Generate new keys";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;
            //Generate a random 128-bit key 
            Random random = new Random();
            if (msgEncrypted)
            {
                result = MessageBox.Show(this, message, caption, buttons,
                                        MessageBoxIcon.Question, MessageBoxDefaultButton.Button1,
                                        MessageBoxOptions.RightAlign);

                if (result == DialogResult.No)
                {
                    return;
                }
                for (int i = 0; i < Block.blockSize; i++)
                {
                    publicKey.setByte((byte)random.Next(0, 255), i);
                    //Console.WriteLine($"Block key #{i} = {publicKey.getByte(i)}");
                }
                keySet = true;
            }
        }

        private int findDegree(int x)
        {
            //find the degree of the polynomiaal represented by the integer x
            if (x == 0) return 0;
            return (int)Math.Floor(Math.Log((double)x) / Math.Log(2));
        }

        private void PolyDiv(int dividend, int divisor, ref int quotient, ref int remainder)
        {
            // a function to take 2 numbers representing the dividend and divisor mod-2 polynomials 
            // and return the integers representing the quotient and the remainder 

            int r1 = 0, q1 = 0, d1 = 0;
            int degree1, degree2;
            int deltad;
            int newDividend;
            int stepCount = 1;     // counts the number of steps in the division process
            //Console.WriteLine(String.Format("0x{0:X4}/0x{1:X4}", dividend, divisor));
            if (divisor == 1)
            {
                remainder = 0;
                quotient = dividend;
                return;
            }
            if (divisor > dividend)
            {
                remainder = dividend;
                quotient = 0;
                return;
            }
            //find degree of dividend and divisor
            newDividend = dividend;
            degree1 = findDegree(newDividend);
            degree2 = findDegree(divisor);
            deltad = degree1 - degree2;
            do
            {
                d1 = divisor << (int)deltad;
                //Console.WriteLine(String.Format(" Step: {0}, deltad : {1}", stepCount, deltad));
                //Console.WriteLine(String.Format("    {0:X2}", newDividend));
                //Console.WriteLine(String.Format("+   {0:X2}", d1));
                //Console.WriteLine(String.Format("-----------"));
                q1 += (int)Math.Pow(2, deltad);
                newDividend = newDividend ^ d1;
                //Console.WriteLine(String.Format("    {0:X2}", newDividend));
                //Console.WriteLine(String.Format("Quotient is now {0}", q1));
                degree1 = findDegree(newDividend);
                deltad = degree1 - degree2;
                stepCount++;
            } while (deltad >= 0);

            quotient = q1;
            remainder = newDividend;
            //Console.WriteLine(String.Format("Returning: Quotient = 0x{0:X2}, Remainder = 0x{1:X2}", quotient, remainder));
        }

        private int PolyMult(int a, int b)
        {
        // Galois Field (256) Multiplication of two Bytes
            int p = 0;

            for (int i = 0; i < 8; i++)
            {
                if ((b & 1) != 0)
                {
                    p ^= a;
                }

                bool hi_bit_set = (a & 0x80) != 0;
                a <<= 1;
                if (hi_bit_set)
                {
                    a ^= 0x1B; /* x^8 + x^4 + x^3 + x + 1 */
                }
                b >>= 1;
            }

            return p;
        }

        private String PrintPoly(int x, int degree)
        {
            //A function that takes two integers representing the polynomial and the degree
            //and prints out a string 
            String output = String.Format("({0:d3}): ", x);
            //Console.WriteLine(String.Format("Degree of polynomial 0x{0:X2} is {1}",x,  degree));
            for (int i = degree; i >= 0; i--)
            {
                if (i == degree && ((int)Math.Pow(2, i)) == (int)Math.Pow(2, i))
                {
                    if (i > 1)
                        output += "x^" + i.ToString();
                    else
                        output += "x";
                }
                else if (i == 0 && x % 2 == 1)
                {
                    output += " + 1";
                }
                else if ((x & (int)Math.Pow(2, i)) == (int)Math.Pow(2, i))
                {
                    if (i > 1)
                    {
                        output += " + x^" + i.ToString();
                    }
                    else
                    {
                        output += " + x";
                    }
                }
            }
            return output;
        }

        public int extendedEuclidGF(int a, int b, ref int x, ref int y)
        {
            //Console.WriteLine("Calling extendedEuclidGF with a={0:X2} and b={1:X2}", a, b);
            // Base Case 
            if (a == 0)
            {
                x = 0;
                y = 1;
                return b;
            }
            int quo = 0, rem = 0;
            PolyDiv(b, a, ref quo, ref rem);
            // To store results of 
            // recursive call 
            int x1 = 0, y1 = 0;
            int gcd = extendedEuclidGF(rem, a, ref x1, ref y1);

            // Update x and y using  
            // results of recursive call 
            x = y1 ^ PolyMult(quo, x1);
            y = x1;

            return gcd;
        }

        public int modInverseGF(int a, int m)
        {
            //return the multiplicative modular inverse of a mod n in GF(2^8)
            int x0 = 0, y0 = 0;
            int res;
            int g = extendedEuclidGF(a, m, ref x0, ref y0);
            if (g != 1)
            {
                Console.WriteLine("Inverse of " + a + " does not exist");
                return -1;
            }
            else
            {
                res = x0;  ////(x0 % m + m) % m;      ///to resolve cases where x0 is negative
            }
            return res;
        }

        private void GenForwardSBox()
        {
            int row, col;
            Console.WriteLine("Starting to generate Forward and inverse Sbox for base Polynomial, " + PrintPoly(rijndaelPoly, 8));
            for (byte i = 0; i < 16; i++)
                for (byte j = 0; j < 16; j++)
                {
                    if (i == 0 && j == 0)
                    {
                        row = 6;
                        col = 3;
                        sBox[i, j] = 0x63;
                        invSBox[row, col] = 0x00;
                    }
                    else
                    {
                        int v = 16 * i + j;
                        byte b = (byte)modInverseGF(v, rijndaelPoly);
                        sBox[i, j] = b ^ ROTL8(b, 1) ^ ROTL8(b, 2) ^ ROTL8(b, 3) ^ ROTL8(b, 4) ^ 99;
                        row = sBox[i,j] / 16;
                        col = sBox[i,j] % 16;
                        invSBox[row, col] = 16 * i + j;
                    }
                    //Console.WriteLine("S-Box");
                    //Console.WriteLine("[{0:X2},{1:X2}] = {2:X2}", i, j, sBox[i,j] );
                    //Console.WriteLine("Inverse S-Box");
                    //Console.WriteLine("[{0:X2},{1:X2}] = {2:X2}", row, col, invSBox[row, col]);

                }
            Console.WriteLine("Completed forward  and inverse SBox for base Polynomial, " + PrintPoly(rijndaelPoly, 8));
            sBoxGenerated = true;

        }


        private byte ROTL8(byte x, byte shift)
        //implements a circular left shift over the byte
        {
            return (byte)(((x) << (shift)) | ((x) >> (8 - (shift))));
        }

        private void ExpandKeySchedule()
        {
            //Expand the keys from the generated public Key
            int i,j,n ;
            byte[] temp = new byte[4];
            byte[] sub_temp = new byte[4];
            byte[] rot_temp = new byte[4];
            Block newKey,oldKey;
            initBlockList(roundKeys, 11);
            if (!keySet)
            {
                MessageBox.Show("You must enter a valid key to begin key expansion");
                return;
            }
            roundKeys[0].setBlock( publicKey);
            byte[] RCON = { 0x0, 0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, 0x80, 0x1B, 0x36 };
            byte[,] w = new byte[44,4];
            newKey = new Block();
            oldKey = roundKeys[0];
            Console.WriteLine("(0): old key=" + oldKey.printBlock());
            for (i = 0; i < 4; i++)
            {
               // Console.Write(String.Format("w[{0}] = ", i));
                for (j = 0; j < 4; j++) {
                    w[i, j] = roundKeys[0].getByte(4 * i + j);
                    //Console.Write(w[i,j].ToString("X"));
                }
                //Console.Write('\n');
            }
            for (i = 4; i < 44; i++)
            {
                for (j = 0; j < 4; j++) { temp[j] = w[i - 1, j]; }
                if (i % 4 == 0)
                {
                    rot_temp = RotWord(temp);
                    //printWord(rot_temp);
                    sub_temp = SubWord(rot_temp);
                    //printWord(sub_temp);

                    for (j = 0; j < 4; j++) //RCON xor step
                    {
                        if (j == 3)  //RCON is only MSB,other thre bytes in word are 0
                        {
                            temp[j] = (byte)(sub_temp[j] ^ RCON[i / 4]);
                        }
                        else
                        {
                            temp[j] = sub_temp[j];
                        }
                    }
                    
                }
                //Console.Write("(" + i + ") temp = ");
                //printWord(temp);
                //Console.Write("w[" + i + "] =");
                n = i / 4;
                for (j = 3; j >=0; j--)
                {
                    w[i, j] = (byte)(w[i - 4, j] ^ temp[j]);
                    //Console.Write(String.Format("{0:X2}",w[i,j]));
                    roundKeys[n].setByte(w[i, j], (4*i + j)%16);
                }
                //Console.Write("(LSB)\n");
                //set Round Key
                if (i%4 == 3)
                {
                    Console.WriteLine(String.Format("roundKeys[{0}] = {1}", n, roundKeys[n].printBlock()));
                }
            }
            
                           
        }

        private void printWord( byte [] word)
        {
            for (int j = 3; j >= 0; j--)
            {
                Console.Write(word[j].ToString("X2"));
            }
            Console.Write("(LSB)\n");
        }

        private byte[] SubWord(byte[] inputWord)
        {
            byte [] outputWord = new byte[4];
            int row, col;
            for ( int i =0; i <4; i++)
            {
                row = inputWord[i] / 16;
                col = inputWord[i] % 16;
                outputWord[i] = (byte) sBox[row, col];
            }
            return outputWord;
        }
        private byte[] RotWord(byte[] inputWord)
        {
            byte[] outputWord = new byte[4];
            for(int i=0; i < 4; i++)
            {
                outputWord[i] = inputWord[(i + 1) % 4];
            }
            return outputWord;
        }

        private List<Block> AddRoundKey(int n, List<Block> inputBlocks)
        {
            List<Block> outputBlocks = new List<Block>();
            initBlockList(outputBlocks, msgLength);
            
            //XOr the round Key from the appropriate round n and place the answer into addBlocks
            for (int i = 0; i < msgLength; i++) {
                
                //Console.WriteLine("("+i+") Input block: " + inputBlocks[i].printBlock() + ", round key = " + roundKeys[n].printBlock());
                for (int j = 0; j < Block.blockSize; j++)
                {
                    outputBlocks[i].setByte((byte) (inputBlocks[i].getByte(j) ^ roundKeys[n].getByte(j)),j);
                }
                //Console.WriteLine("Add block: " + outputBlocks[i].printBlock());

            }
            return outputBlocks;
        }

        private List<Block> SubBytes(List<Block> inputBlocks)
        {
            List<Block> outputBlocks = new List<Block>();
            initBlockList(outputBlocks, msgLength);
            Block subBlock = new Block();
            int row, col;

            if (!sBoxGenerated)
            {
                MessageBox.Show("Error: Sbox is not generated: ");
                return outputBlocks;
            }
            if (inputBlocks.Count != msgLength)
            {
                MessageBox.Show("Error: inputBlocks is not message length ");
                return outputBlocks;
            }
            for (int i = 0; i < msgLength; i++) {
                //Console.WriteLine("(" + i + ") Input block: " + inputBlocks[i].printBlock());
                for (int j = 0; j < Block.blockSize; j++)
                {
                    row =  inputBlocks[i].getByte(j) / 16;
                    col = inputBlocks[i].getByte(j) % 16;
                    //Console.WriteLine($"row:{row:X2}, col: {col:X2}, sbox: {sbox[row,col]:X2}");
                    outputBlocks[i].setByte((byte)sBox[row, col],j);
                }
              
                Console.WriteLine("subblock: " + outputBlocks[i].printBlock());
            }
            return outputBlocks;
        }

        private List<Block> InvSubBytes(List<Block> inputBlocks)
        {
            List<Block> outputBlocks = new List<Block>();
            initBlockList(outputBlocks, msgLength);
            Block subBlock = new Block();
            int row, col;

            if (!sBoxGenerated)
            {
                MessageBox.Show("Error: Sbox is not generated: ");
                return outputBlocks;
            }
            if (inputBlocks.Count != msgLength)
            {
                MessageBox.Show("Error: inputBlocks is not message length ");
                return outputBlocks;
            }
            for (int i = 0; i < msgLength; i++)
            {
                //Console.WriteLine("(" + i + ") Input block: " + inputBlocks[i].printBlock());
                for (int j = 0; j < Block.blockSize; j++)
                {
                    row = inputBlocks[i].getByte(j) / 16;
                    col = inputBlocks[i].getByte(j) % 16;
                    //Console.WriteLine($"row:{row:X2}, col: {col:X2}, sbox: {sbox[row,col]:X2}");
                    outputBlocks[i].setByte((byte)invSBox[row, col], j);
                }

                Console.WriteLine("subblock: " + outputBlocks[i].printBlock());
            }
            return outputBlocks;

        }


        private List<Block> ShiftRows(List<Block> inputBlocks)
        {
            
            List<Block> outputBlocks = new List<Block>();
            initBlockList(outputBlocks, msgLength);
            int newindex;
            //process take 16 bit block and shift it
            for(int i =0; i < inputBlocks.Count; i++) 
            {
                for (int j = 0; j < Block.blockSize; j++)
                {
                    if (j < 4)
                    {
                        newindex = j;
                        outputBlocks[i].setByte(inputBlocks[i].getByte(j), j);
                    }
                    else if (j < 8)
                    {
                        newindex = ((j - 1) % 4) + 4;
                        outputBlocks[i].setByte(inputBlocks[i].getByte(j), newindex);
                    }
                    else if (j < 12)
                    {
                        newindex = ((j - 2) % 4) + 8;
                        outputBlocks[i].setByte(inputBlocks[i].getByte(j), newindex);
                    }
                    else
                    {
                        newindex = ((j - 3) % 4) + 12;
                        outputBlocks[i].setByte(inputBlocks[i].getByte(j), newindex);
                    }
                    //if (i == 0) Console.WriteLine(String.Format("j = {0}, newindex  = {1}", j, newindex)); 
                }
                //Console.WriteLine("shift block: " + outputBlocks[0].printBlock());

            }
            return outputBlocks;
            
        }

        private List<Block> InvShiftRows(List<Block> inputBlocks)
        {

            List<Block> outputBlocks = new List<Block>();
            initBlockList(outputBlocks, msgLength);
            int newindex;
            //process take 16 bit block and shift it
            for (int i = 0; i < inputBlocks.Count; i++)
            {
                for (int j = 0; j < Block.blockSize; j++)
                {
                    if (j < 4)
                    {
                        newindex = j;
                        outputBlocks[i].setByte(inputBlocks[i].getByte(j), j);
                    }
                    else if (j < 8)
                    {
                        newindex = ((j + 1) % 4) + 4;
                        outputBlocks[i].setByte(inputBlocks[i].getByte(j), newindex);
                    }
                    else if (j < 12)
                    {
                        newindex = ((j + 2) % 4) + 8;
                        outputBlocks[i].setByte(inputBlocks[i].getByte(j), newindex);
                    }
                    else
                    {
                        newindex = ((j + 3) % 4) + 12;
                        outputBlocks[i].setByte(inputBlocks[i].getByte(j), newindex);
                    }
                    //if (i == 0) Console.WriteLine(String.Format("j = {0}, newindex  = {1}", j, newindex)); 
                }
                //Console.WriteLine("shift block: " + outputBlocks[0].printBlock());

            }
            return outputBlocks;

        }

        private List<Block> MixColumns(List<Block> inputBlocks)
        {
            List<Block> outputBlocks = new List<Block>();
            initBlockList(outputBlocks, msgLength);
            byte[,] mixMatrix = { { 2, 3, 1, 1 }, { 1, 2, 3, 1 }, { 1, 1, 2, 3 }, { 3, 1, 1, 2 } };
            Block inputBlock = new Block();
            byte temp; 
            int newterm;
            int q0 = 0, r0 = 0;
            int i, j, k;
            for (i = 0; i < inputBlocks.Count; i++)
            {
                //Console.WriteLine("(" + i + ") Input block: " + inputBlocks[i].printBlock());
                inputBlock = inputBlocks[i];
                for (j = 0; j < Block.blockSize; j++)
                {
                    temp = 0;                    
                    //multiply (j%4)th column of inputBlocks[i] by (j/4)th row of mixMatrix 
                    for (k =0; k < 4; k++)
                    {
                        newterm = PolyMult(mixMatrix[(j / 4), k], inputBlock.getByte((j % 4) + 4 * k));
                       // PolyDiv(newterm , rijndaelPoly,ref q0, ref r0);
                        temp ^= (byte)newterm;
                        //Console.WriteLine(String.Format("k = {0}, newterm = {1:X2}, temp ^ newterm = {2:X2}",k, newterm,temp));
                    }

                    //place result byte in position j of mixBlocks{i]
                    //Console.WriteLine(String.Format("({0},{1}) temp = {2:X2}", j/4, j %4, temp));
                    outputBlocks[i].setByte(temp, j);
                    
                }
                Console.WriteLine("mix block: " + outputBlocks[i].printBlock());
            }
            return outputBlocks;
        }

        private List<Block> InvMixColumns(List<Block> inputBlocks)
        {
            List<Block> outputBlocks = new List<Block>();
            initBlockList(outputBlocks, msgLength);
            byte[,] mixMatrix = { { 0x0E, 0x0B, 0x0D, 0x09 }, { 0x09, 0x0E, 0x0B, 0x0D }, { 0x0D, 0x09, 0x0E, 0x0B } , { 0x0B, 0x0D, 0x09, 0x0E } };
            Block inputBlock = new Block();
            byte temp;
            int newterm;
            int q0 = 0, r0 = 0;
            int i, j, k;
            for (i = 0; i < inputBlocks.Count; i++)
            {
                //Console.WriteLine("(" + i + ") Input block: " + inputBlocks[i].printBlock());
                inputBlock = inputBlocks[i];
                for (j = 0; j < Block.blockSize; j++)
                {
                    temp = 0;
                    //multiply (j%4)th column of inputBlocks[i] by (j/4)th row of mixMatrix 
                    for (k = 0; k < 4; k++)
                    {
                        newterm = PolyMult(mixMatrix[(j / 4), k], inputBlock.getByte((j % 4) + 4 * k));
                        // PolyDiv(newterm , rijndaelPoly,ref q0, ref r0);
                        temp ^= (byte)newterm;
                       // Console.WriteLine(String.Format("k = {0}, newterm = {1:X2}, temp ^ newterm = {2:X2}", k, newterm, temp));
                    }

                    //place result byte in position j of mixBlocks{i]
                    //Console.WriteLine(String.Format("({0},{1}) temp = {2:X2}", j / 4, j % 4, temp));
                    outputBlocks[i].setByte(temp, j);

                }
                Console.WriteLine("inverse mix block: " + outputBlocks[i].printBlock());
            }
            return outputBlocks;
        }

        private void encryptAES()
        {
            Console.WriteLine("Encryption begun.");
            addBlocks = AddRoundKey(0,pTBlocks);
            for (int i =1; i< 10; i++)
            {
                subBlocks = SubBytes(addBlocks);
                shiftBlocks = ShiftRows(subBlocks);
                mixBlocks = MixColumns(shiftBlocks);
                addBlocks = AddRoundKey(i, mixBlocks);
            }
            //Final Round
            subBlocks = SubBytes(addBlocks);
            shiftBlocks = ShiftRows(subBlocks);
            addBlocks = AddRoundKey(10,shiftBlocks);
            msgEncrypted = true;
            cTBlocks = addBlocks;
            Console.WriteLine("Encryption complete");
        }

        private void decryptAES()
        {
            Console.WriteLine("Decryption begun");
            addBlocks = AddRoundKey(10, cTBlocks);
            shiftBlocks = InvShiftRows(addBlocks);
            subBlocks = InvSubBytes(shiftBlocks);
            for(int i=9; i>0; i--)
            {
                addBlocks = AddRoundKey(i, subBlocks);
                mixBlocks = InvMixColumns(addBlocks);
                shiftBlocks = InvShiftRows(mixBlocks);
                subBlocks = InvSubBytes(shiftBlocks);
            }
            addBlocks = AddRoundKey(0, subBlocks);
            pTBlocks = addBlocks;
            Console.WriteLine("Decryption complete");
        }

        public void initBlockList(List<Block> blist, int len)
        {
            for( int i = 0; i < len; i++)
            {
                blist.Add(new Block());
            }
        }

       
    }


    public class Block
    {
        
        public const int blockSize = 16;
        private byte[] block = new byte[blockSize];

        public Block()
        {
           for(int i =0; i < blockSize; i++)
            {
                setByte(0, i);
            }
        }
        public void setByte(byte val, int i)
        {
            block[i] = val;
        }

        public void setBlock(Block b)
        {
            block = b.block;
        }

        public byte getByte(int i)
        {
            return block[i];
        }

        public String printBlock()
        {
            String blockOutput = "";
            for (int i = 0; i < blockSize; i++)
            {
                blockOutput += getByte(i).ToString("X2");      //MSB-LSB left to right
            }
            
            return  blockOutput;

        }
        public void readBlock(string inputString)
            //reads a string and inputs it into a block
        {
            byte val;
            if (inputString.Length < 32)
            {
                inputString = inputString.PadLeft(32);
            }
            string byteString;
            for (int i = 0; i <  blockSize ; i++)
            {
                byteString = inputString.Substring(2 * i, 2);
                //Console.WriteLine(byteString);
                if( Byte.TryParse(byteString, System.Globalization.NumberStyles.HexNumber, null,  out val))
                {
                    setByte(val, i);
                } else
                {
                    MessageBox.Show("Characters of string were not valid hex characters (0-9 and A-F).");
                    return;
                }
                                   
            }
            //Console.WriteLine("String successfully read :" + printBlock());
        }

    }
}
