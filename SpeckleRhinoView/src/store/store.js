import Vue from 'vue'
import Vuex from 'vuex'

Vue.use( Vuex )

export default new Vuex.Store( {
  state: {
    accounts: [ ],
    clients: [ ],
    selection: [ ],
    layerInfo: [ ],
    globalLoading: false
  },
  getters: {
    accounts: state => state.accounts,
    clients: state => state.clients,
    clientByStreamId: state => streamId => {
      return state.clients.find( c => c.stream.streamId === streamId )
    },
    selection: state => state.selection,
    layerInfo: state => state.layerInfo
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
        .catch( err => {} )
    },
    removeClient( context, payload ) {
      console.log( 'removing: ' + payload.clientId )
      Interop.removeClient( payload.clientId )
        .then( res => {
          console.log( res )
          context.commit( 'REMOVE_CLIENT', payload.clientId )
        } )
        .catch( err => {} )
    }
  },
  mutations: {
    SET_GL_LOAD( state, payload ) {
      state.globalLoading = payload
    },
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
      console.log( payload )
      payload.client.stream = payload.stream
      // extra props for the ui
      payload.client.log = [ { timestamp: new Date( ), message: 'Client added.' } ]
      payload.client.isLoading = false
      payload.client.error = null
      payload.client.expired = false
      payload.client.lastUpdate = new Date( )
      payload.client.progressMessage = null
      state.clients.unshift( payload.client )
    },
    SET_CLIENT_CHILDREN( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if ( !client ) return console.warn( 'No client found! ' + payload.streamId )
      console.log( payload )
      client.stream.children = payload.data.children
    },
    PURGE_CLIENTS( state ) {
      state.clients = [ ]
    },
    REMOVE_CLIENT( state, payload ) {
      state.clients = state.clients.filter( client => client.ClientId !== payload )
    },
    SET_CLIENTS( state, payload ) {

    },
    APPEND_LOG( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if ( !client ) return console.warn( 'No client found! ' + payload.streamId )
      if ( !client.log ) client.log = [ ]
      client.log.unshift( { timestamp: new Date( ), message: payload.data } )
      if ( client.log.length > 5 ) {
        client.log.pop( )
      }
    },
    SET_PROGRESS_MESSAGE( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if ( !client ) return console.warn( 'No client found! ' + payload.streamId )
      client.progressMessage = payload.message
    },
    SET_ERROR( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if ( !client ) return console.warn( 'No client found! ' + payload.streamId )
      client.error = payload.data
    },
    SET_LOADING( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if ( !client ) return console.warn( 'No client found! ' + payload.streamId )

      client.isLoading = payload.status
      if ( !payload.status )
        client.progressMessage = null
    },
    SET_EXPIRED( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if ( !client ) return console.warn( 'No client found! ' + payload.streamId )

      client.expired = payload.status
    },
    SET_METADATA( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if ( !client ) return console.warn( 'No client found! ' + payload.streamId )

      client.stream.name = payload.stream.name
      client.stream.layers = payload.stream.layers
      client.stream.objects = payload.stream.objects

      client.lastUpdate = new Date( )
      client.expired = false
    },
    SET_SELECTION( state, payload ) {
      state.selection = payload.selectionInfo
    },
    SET_LAYERINFO( state, payload ) {
      state.layerInfo = payload
    }
  }
} )