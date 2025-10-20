﻿namespace SoundWaveShared.Dtos
{
    public class SearchRequestDto
    {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
