﻿using MiniBiggy.UniversalApps;

namespace MiniBiggy {
    public static class PersistentList {
        public static PersistentList<T> Create<T>() where T : new() {
            return new PersistentList<T>(new Storage(), null);
        } 
    }
}
