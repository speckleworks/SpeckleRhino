import Vue from 'vue'
import Vuex from 'vuex'

Vue.use( Vuex )

export default new Vuex.Store( {
  state: {
    accounts: [ ],
    clients: [ ]
  },
  getters: {
    accounts: state => state.accounts,
    clients: state => state.clients
  },
  actions: {
    getUserAccounts( context ) {
      Interop.getUserAccounts( )
        .then( res => {
          context.commit( 'SET_ACCOUNTS', JSON.parse( res ) )
        } )
        .catch( err => {} )
    },
    getFileStreams( context ) {
      Interop.getFileStreams( )
        .then( res => {
          context.commit( 'SET_CLIENTS', JSON.parse( res ) )
        } )
        .catch( err => { } )
    },
    removeClient( context, payload ) {
      console.log( 'removing: '  + payload.clientId )
      Interop.removeClient( payload.clientId )
      .then( res => {
        console.log( res )
        context.commit( 'REMOVE_CLIENT', payload.clientId )
      })
      .catch( err => { })
    }
  },
  mutations: {
    SET_ACCOUNTS( state, payload ) {
      state.accounts = payload
    },
    ADD_ACCOUNTS( state, payload ) {
      state.accounts = [ ...state.accounts, ...payload ]
    },
    DELETE_ACCOUNT( state, payload ) {
      state.accounts = state.accounts.filter( item => item.fileName !== payload )
    },
    ADD_CLIENT( state, payload ) {
      payload.client.stream = payload.stream
      payload.client.log = [ { timestamp: new Date(), message: 'Client added.' } ]
      state.clients.unshift( payload.client )
    },
    REMOVE_CLIENT( state, payload ) {
      console.log( state.clients )
      state.clients = state.clients.filter( client => client.ClientId !== payload )
      console.log( state.clients )
    },
    SET_CLIENTS( state, payload ) {

    },
    APPEND_LOG( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if( !client ) return console.warn( 'No client found!' )

      client.log.unshift( { timestamp: new Date(), message: payload.data } )
      if ( client.log.length > 42 ) log.pop( )
    }
  }
} )