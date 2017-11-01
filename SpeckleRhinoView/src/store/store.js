import Vue from 'vue'
import Vuex from 'vuex'

Vue.use( Vuex )

export default new Vuex.Store( {
  state: {
    accounts: [ ],
    clients: [ ]
  },
  getters: {
    accounts: state => state.accounts
  },
  actions: {

  },
  mutations: {
    SET_ACCOUNTS( state, payload ) {
      state.accounts = payload
    },
    ADD_ACCOUNTS( state, payload ) {
      state.accounts = [ ...state.accounts, ...payload ]
    },
    DELETE_ACCOUNT( state, payload ) {
      console.log( payload )
      state.accounts = state.accounts.filter( item => item.fileName !== payload )
    }
  }
} )