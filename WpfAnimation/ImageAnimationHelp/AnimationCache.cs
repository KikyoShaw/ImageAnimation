using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;

namespace WpfAnimation.ImageAnimationHelp
{
    public class AnimationCacheEntry
    {
        public AnimationCacheEntry(ObjectKeyFrameCollection keyFrames, Duration duration)
        {
            KeyFrames = keyFrames;
            Duration = duration;
        }

        public ObjectKeyFrameCollection KeyFrames { get; }
        public Duration Duration { get; }
    }

    public class AnimationCache
    {
        private static readonly Lazy<AnimationCache> Lazy = new Lazy<AnimationCache>(() => new AnimationCache());
        public static AnimationCache Instance => Lazy.Value;

        private readonly ConcurrentDictionary<string, AnimationCacheEntry> _cacheEntries =
            new ConcurrentDictionary<string, AnimationCacheEntry>();

        /// <summary>
        /// 判断当前sourceKey的帧动画是否已经在缓存中
        /// </summary>
        /// <param name="sourceKey"></param>
        /// <returns>是否缓存</returns>
        public bool IsCached(string sourceKey)
        {
            return _cacheEntries.ContainsKey(sourceKey);
        }

        /// <summary>
        /// 添加动画帧
        /// </summary>
        /// <param name="sourceKey">动画帧缓存的key</param>
        /// <param name="keyFrames">动画所有帧</param>
        /// <returns>是否添加成功</returns>
        public bool AddKeyFrames(string sourceKey, List<KeyFrame> keyFrames)
        {
            var keyFrameCollection = new ObjectKeyFrameCollection();
            var totalDuration = TimeSpan.Zero;
            foreach (var item in keyFrames)
            {
                totalDuration += TimeSpan.FromMilliseconds(item.Duration);
                var keyFrame = new DiscreteObjectKeyFrame(item.AFrame, totalDuration);
                keyFrameCollection.Add(keyFrame);
            }

            var cacheEntry = new AnimationCacheEntry(keyFrameCollection, totalDuration);
            return _cacheEntries.TryAdd(sourceKey, cacheEntry);
        }

        /// <summary>
        /// 获取缓存中的帧动画
        /// </summary>
        /// <param name="sourceKey">帧动画缓存的key</param>
        /// <param name="entry">帧动画缓存</param>
        /// <returns></returns>
        internal bool GetCacheEntry(string sourceKey, out AnimationCacheEntry entry)
        {
             return _cacheEntries.TryGetValue(sourceKey, out entry);
        }
    }
}
