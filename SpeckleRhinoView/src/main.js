import Vue from 'vue'
import Vuex from 'vuex'
import App from './App.vue'
import VueMaterial from 'vue-material'

import Store from './store/store.js'

Vue.use( Vuex )
Vue.use( VueMaterial )

Vue.material.registerTheme('default', {
  primary: 'black',
  accent: 'light-blue',
  warn: 'red',
  background: 'white'
})

new Vue( {
  store: Store,
  el: '#app',
  render: h => h( App )
} )