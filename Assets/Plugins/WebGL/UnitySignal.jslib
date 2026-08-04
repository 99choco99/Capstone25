var UnitySignals = {

    MySignalReady: function() {
        if (typeof window.onUnityReady === 'function') {
            window.onUnityReady();
        }
    }
};

// Unity 네이티브 라이브러리에 함수를 추가
mergeInto(LibraryManager.library, UnitySignals);