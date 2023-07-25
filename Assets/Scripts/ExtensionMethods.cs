using System.Collections.Generic;

namespace UnityLab
{
    public static class ExtensionMethods
    {
        /// <summary>
        ///     Given array of elements [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16] we know that in the grid representation
        ///     (i.e. spritesheet) 1,2,3,4 and 9,10,11,12 are one group. 5,6,7,8 and 13,14,15,16 are the other group.
        ///     So given the array of elements, width of 4 and a height of 2, for the array above this function converts it into
        ///     [[[1,2,3,4],[9,10,11,12]], [[5,6,7,8], [13,14,15,16]]]
        /// </summary>
        public static List<T[,]> Convert1DArrayInto3DArray<T>(this T[] array, int width, int height)
        {
            var totalNumberOfElementsPer2DArray = width * height;
            var numberOf2DArrays = (array.Length + totalNumberOfElementsPer2DArray - 1) / totalNumberOfElementsPer2DArray;
            var listOf2DArrays = new List<T[,]>(numberOf2DArrays);

            for (int i = 0; i < array.Length; i++)
            {
                // Calculates the current 2D array, row, and column
                var currentArrayNumber = i / totalNumberOfElementsPer2DArray;
                var row = (i / width) % height;
                var col = i % width;

                // Creates a new 2D array if the current array is full
                if (i % totalNumberOfElementsPer2DArray == 0)
                    listOf2DArrays.Add(new T[height, width]);

                // Assigns the current element to the calculated position
                listOf2DArrays[currentArrayNumber][row, col] = array[i];
            }

            return listOf2DArrays;
        }
    }
}