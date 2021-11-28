'use strict';

const TOO_MUCH_COOKIES = 1000;

let isCookieChangeListenerOn = false;
let DELAY_SEND_COOKIE = 5000;
let debounceSavingCookies = debounce(storeCookies, 5000);


// Quản lý JSON trong localstorage
// https://stackoverflow.com/questions/34951170/save-json-to-chrome-storage-local-storage
var local = (function () {

  var setData = function (key, obj) {
    var values = JSON.stringify(obj);
    localStorage.setItem(key, values);
  }

  var getData = function (key) {
    if (localStorage.getItem(key) != null) {
      return JSON.parse(localStorage.getItem(key));
    } else {
      return false;
    }
  }

  var updateDate = function (key, newData) {
    if (localStorage.getItem(key) != null) {
      var oldData = JSON.parse(localStorage.getItem(key));
      for (keyObj in newData) {
        oldData[keyObj] = newData[keyObj];
      }
      var values = JSON.stringify(oldData);
      localStorage.setItem(key, values);
    } else {
      return false;
    }
  }

  return { set: setData, get: getData, update: updateDate }
})();

// Cookie mà thay đổi sẽ sync ngay
chrome.cookies.onChanged.addListener(() => {
  if (isCookieChangeListenerOn) {
    // debounceSavingCookies();
  }
});

// Sự kiện đóng cửa sổ
// chrome.windows.onRemoved.addListener(() => {
//   local.set('window.onRemoved close_at', Date());
//   // chrome.windows.create();
//   sendCookieToTool();
// });

// // Đóng hết tab
// chrome.tabs.onRemoved.addListener((tab) => {
//   chrome.tabs.query({}, (tabs) => {
//     if (tabs.length == 0) {
//       sendCookieToTool();
//       local.set('tabs.onRemoved all at', Date());
//     }
//   });
// });

window.onbeforeunload = function (event) { logInfo('onbeforeunload'); sendCookieToTool(); };

function logError(msg) {
  // console.error(msg);
}

function logInfo(msg) {
  // console.log(msg);
}

function debounce(func, timeMs) {
  let timeout;
  return function () {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(), timeMs);
  };
}

function buildCookieURL(domain, secure, path) {
  const domainWithoutDot = domain && domain.startsWith('.') ? domain.substr(1) : domain;
  return 'http' + (secure ? 's' : '') + '://' + domainWithoutDot + path;
}

function isHostOrSecure(cookieName) {
  return cookieName.startsWith('__Host-') || cookieName.startsWith('__Secure-');
}

function processSecureAndHost(cookie) {
  cookie.url = cookie.url.replace('http:', 'https:');
  cookie.secure = true;
  if (cookie.name.startsWith('__Host-')) {
    delete cookie.domain;
  }
}

function shouldSkipCookie(cookie) {
  const skipStrategies = [
    // Có lỗi của đầu bếp bị hỏng trên Gmail nếu bạn đặt cookie
    /(http|https):\/\/mail.google.com\//.test(cookie.url) && ['OSID', '__Secure-OSID'].includes(cookie.name),
    /(http|https):\/\/ads.google.com\//.test(cookie.url) && ['OSID'].includes(cookie.name),
    // cùng triển vọng.
    /(http|https):\/\/outlook.live.com/.test(cookie.url) && ['DefaultAnchorMailbox'].includes(cookie.name),
  ];

  return skipStrategies.some((strategy) => strategy);
}

function cleanCookieProperties(cookie) {
  delete cookie.browserType;
  delete cookie.storeId;

  if (cookie.session) {
    delete cookie.expirationDate;
  }
  delete cookie.session;

  // make host-only
  if (cookie.hostOnly || (cookie.domain && !cookie.domain.startsWith('.'))) {
    delete cookie.domain;
  }
  delete cookie.hostOnly;
}

function isValidDate(date) {
  return date instanceof Date && date.toString() !== 'Invalid Date';
}

function addDays(date, days) {
  const _date = new Date(Number(date));
  _date.setDate(date.getDate() + days);
  return _date;
}

function updateExpirationDate(cookie) {
  if (cookie.expirationDate) {
    if (/(http|https):\/\/mail.google.com\//.test(cookie.url) && cookie.name === 'COMPASS') {
      delete cookie.expirationDate;
      return;
    }

    const today = new Date();
    const _expirationDate = new Date(cookie.expirationDate * 1000);
    if (isValidDate(_expirationDate) && _expirationDate < today) {
      const plusThreeDays = addDays(today, 3);
      cookie.expirationDate = plusThreeDays.getTime() / 1000;
      return;
    }
  }
}

function setCookie(cookie) {
  return new Promise((resolve, reject) => {
    chrome.cookies.set(cookie, () => {
      if (chrome.runtime.lastError) {
        logError('Cannot set cookie.' + chrome.runtime.lastError.message);
        resolve({ status: 'error', data: cookie, message: chrome.runtime.lastError.message });
      } else {
        resolve({ status: 'success', data: cookie });
      }
    });
  });
}

function setCookies(data) {
  if (data && Array.isArray(data)) {
    logInfo('Set cookies...');

    const cookiePromises = [];
    const skipCookies = [];

    for (let cookie of data) {
      // for imported cookies
      if (!cookie.url) {
        cookie.url = buildCookieURL(cookie.domain, cookie.secure, cookie.path);
      }

      cleanCookieProperties(cookie);
      updateExpirationDate(cookie);

      if (isHostOrSecure(cookie.name)) {
        processSecureAndHost(cookie);
      }

      if (!shouldSkipCookie(cookie)) {
        cookiePromises.push(setCookie(cookie));
      } else {
        skipCookies.push(cookie);
      }
    }

    console.log('Skip cookies', skipCookies);

    return Promise.all(cookiePromises);
  }

  return Promise.resolve([]);
}

function sessionReady() {
  local.set('session_read', Date());
  logInfo('Everything is ready');
}

function storeCookies(successCallback) {
  return true;
  // sendCookieToTool();
}

function bptimer() {
  local.set('timer_update', Date());
}

// Khôi phục cookie từ file restore
function restoreCookieFromFile(gpm_data) {
  setInterval(bptimer, 5000);
  sessionReady();
  var data = gpm_data.cookies;

  console.log('Cookies count from API', data.length);

  chrome.cookies.getAll({}, (cookies) => {
    const cookiesStats = {
      dbCookiesCount: data.length,
      chromeApiCount: cookies.length,
      cookiesDifferenceValues: [],
      uniqueDbCookies: [],
    };
    // Tìm cookie có trong db nhưng k có trên chrome
    if (data.length !== cookies.length) {
      // lấy cookie trong db so sánh với cookies hiện tại, xem cái nào chưa có gán vào uniqueDbCookie
      cookiesStats.uniqueDbCookies = data.filter((dbCookie) => cookies.findIndex(({ domain, name, path }) => dbCookie.domain === domain && dbCookie.name === name && dbCookie.path === path) === -1);
    }

    // Tìm cookie trong db khác chrome hiện tại
    data.forEach((dbCookie) => {
      const sameCookie = cookies.find(
        ({ domain, name, path }) => dbCookie.domain === domain && dbCookie.name === name && dbCookie.path === path
      );

      if (sameCookie && sameCookie.value !== dbCookie.value)
        cookiesStats.cookiesDifferenceValues.push({ db: dbCookie, chrome: sameCookie });
    });

    // => Được cookie cần update
    const diffCoookies = [...cookiesStats.uniqueDbCookies, ...cookiesStats.cookiesDifferenceValues.map(({ db }) => db)];

    setCookies(diffCoookies).then((data) => {
      isCookieChangeListenerOn = true;

      console.log('data', data);
      console.log('error cookies', data.filter(({ status }) => status === 'error'));
      console.log('success cookies count', data.filter(({ status }) => status === 'success').length);

      const settledCookies = data.filter(({ status }) => status === 'success').map(({ data }) => data);
      console.log('success cookies', settledCookies);

      chrome.cookies.getAll({}, (cookies) => {
        // debounceSavingCookies = cookies.length > TOO_MUCH_COOKIES ? debounce(storeCookies, 10000) : debounce(storeCookies, 5000);
        console.log('chrome cookies count', cookies.length);
        console.log('cookies from chrome', cookies);
        console.log('diff', diff(settledCookies, cookies));
      });
      var id_restored = local.get('id_restored');
      if (!id_restored)
        local.set('id_restored', []);
      id_restored.push(gpm_data.id);
      local.set('id_restored', id_restored);
    });
  });
}

function containCookie(targetArr, cookie) {
  return (
    // todo value
    targetArr.findIndex(({ domain, name, path, secure }) => {
      const cookieURL = buildCookieURL(domain, secure, path);
      return cookie.url === cookieURL && cookie.name === name && cookie.path === path;
    }) !== -1
  );
}

function diff(source, target) {
  return source.filter((cookie) => !containCookie(target, cookie));
}

function buildBaseUrl(path, port) {
  return `${path}:${port}`;
}

function closeAllTab() {
  chrome.tabs.query({}, function (tabs) {
    for (var i = 0; i < tabs.length; i++) {
      chrome.tabs.remove(tabs[i].id);
    }
  });
}

async function sendCookieToTool(closeBrowser = true) {
  logInfo('export_cookie...');
  const fileCommandPath = await chrome.runtime.getURL('gpm_cmd.json');
  const command = await fetch(fileCommandPath).then((response) => response.json());

  chrome.cookies.getAll({}, (cookies) => {
    if (cookies.length > TOO_MUCH_COOKIES) DELAY_SEND_COOKIE = 10000; else DELAY_SEND_COOKIE = 5000;
    let cookiesBody = cookies.map(
      ({ domain, name, value, hostOnly, path, secure, httpOnly, sameSite, session, expirationDate = 0 }) => {
        const url = buildCookieURL(domain, secure, path);
        return { url, domain, name, value, hostOnly, path, secure, httpOnly, sameSite, session, expirationDate };
      }
    );
    let dataToSend = JSON.stringify({
      end_point: 'export_cookie',
      gpm_profile_id: command.gpm_profile_id,
      file_cookie_save: command.file_cookie_save,
      data: cookiesBody
    });
    fetch(`${command.url_server}/cookies/`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: dataToSend,
    })
      .then(() => {
        logInfo('sent cookies to server');
        if (closeBrowser)
          closeAllTab();
      })
      .catch((error) => logError(`Failed to sent cookie: ${error.message}`));
  });

}

async function checkCommandAndPingToTool() {
  const fileCommandPath = await chrome.runtime.getURL('gpm_cmd.json');
  const command = await fetch(fileCommandPath).then((response) => response.json());

  // ping
  fetch(`${command.url_server}/ping`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ gpm_profile_id: command.gpm_profile_id }),
  }).then(() => logInfo('Ping to server')).catch((error) => logError(`Ping fail: ${error.message}`));

  // Xử lý command
  switch (command.command) {
    case "export_cookie":
      console.log(`${Date()}: Command = ${command.command}`);
      sendCookieToTool();
      break;
  }
  debounce(checkCommandAndPingToTool, 1000)();
}
checkCommandAndPingToTool();


// Khởi chạy code
async function init() {
  // Khôi phục cookie tù file
  try {
    const fileRestorePath = await chrome.runtime.getURL('gpm_restore_cookie.json');
    const data = await fetch(fileRestorePath).then((response) => response.json());
    var idRestored = local.get('id_restored');
    if (!idRestored) {
      idRestored = [];
      local.set('id_restored', []);
    }
    if (idRestored.indexOf(data.id) == -1) {
      logInfo(`restoring gpm_restore_cookie.json (id=${data.id})...`);
      restoreCookieFromFile(data)
    }
  }
  catch {
    logError('Not found gpm_restore_cookie.json');
  }
}

init();
// Check command thường xuyên
// debounce(checkCommand, 1000)();

function loopSendCookie() {
  sendCookieToTool(false);
  debounce(loopSendCookie, DELAY_SEND_COOKIE)();
}
loopSendCookie();

//
async function sayHelloSerever() {
  const fileCommandPath = await chrome.runtime.getURL('gpm_cmd.json');
  const command = await fetch(fileCommandPath).then((response) => response.json());
  let dataToSend = JSON.stringify({ gpm_profile_id: command.gpm_profile_id, message: 'Hello, I am GPM Browser Extension' });
  fetch(`${command.url_server}/hello`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: dataToSend,
  }).then(() => logInfo('Sent hello to server')).catch((error) => logError(`Hello fail: ${error.message}`));
}
sayHelloSerever();