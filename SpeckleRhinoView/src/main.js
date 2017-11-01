import Vue from 'vue'
import Vuex from 'vuex'
import App from './App.vue'

import Vuetify from 'vuetify'



import Store from './store/store.js'

Vue.use( Vuex )
Vue.use( Vuetify )

new Vue( {
  store: Store,
  el: '#app',
  render: h => h( App )
} )