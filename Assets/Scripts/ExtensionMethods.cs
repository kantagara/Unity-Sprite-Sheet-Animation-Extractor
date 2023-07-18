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
        public static List<T[,]> Convert1DArrayInto2DArray<T>(this T[] array, int width, int height)
        {
            var numberOfElements = array.Length;
            var totalNumberOfElementsPer2DArray = width * height;
            var numberOf2DArrays = (numberOfElements + totalNumberOfElementsPer2DArray - 1) /
                                   totalNumberOfElementsPer2DArray;

            var listOf2DArrays = new List<T[,]>(numberOf2DArrays);

            for (var currentArrayNumber = 0; currentArrayNumber < numberOf2DArrays; currentArrayNumber++)
            {
                var rowOffset = 0;
                var currentArray = new T[height, width];

                for (var i = 0; i < height; i++)
                {
                    for (var j = 0; j < width; j++)
                        currentArray[i, j] = array[i + currentArrayNumber * width + rowOffset + j];

                    rowOffset += numberOf2DArrays * width - 1;
                }

                listOf2DArrays.Add(currentArray);
            }

            return listOf2DArrays;
        }
    }
}