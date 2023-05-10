using System.Collections.Generic;

namespace DefaultNamespace
{
    public static class ExtensionMethods
    {
        public static List<T[,]> Convert1DArrayInto2DArray<T>(this T[] array, int width, int height)
        {
            var numberOfElements = array.Length;
            var totalNumberOfElementsPer2DArray = width * height;
            var numberOf2DArrays = (numberOfElements + totalNumberOfElementsPer2DArray - 1) /
                                   totalNumberOfElementsPer2DArray;

            var listOf2DArrays = new List<T[,]>(numberOf2DArrays);
            
            for (int currentArrayNumber = 0; currentArrayNumber < numberOf2DArrays; currentArrayNumber++)
            {
                var rowOffset = 0;
                var currentArray = new T[height, width];

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        currentArray[i, j] = array[i + currentArrayNumber * width + rowOffset + j];
                    }

                    rowOffset += numberOf2DArrays * width - 1;
                }
                listOf2DArrays.Add(currentArray);
            }

            return listOf2DArrays;

        }
    }
}