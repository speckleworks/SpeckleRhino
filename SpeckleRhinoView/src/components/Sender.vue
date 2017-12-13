<template>
  <v-card class='receiver-content'>
    <!-- header - menu and title -->
    <v-layout>
      <!-- speed dial menu -->
      <v-flex class='xs2'>
        <v-speed-dial v-model='fab' direction='right' left style='top:15px' class='pa-0 ma-0'>
          <v-btn fab small class='ma-0 purple' slot='activator' v-model='fab'>
            <v-icon xxxclass='pink--text xxxxs-actions'>
              cloud_download
            </v-icon>
            <v-icon>close</v-icon>
          </v-btn>
          <v-btn fab small class='light-blue'>
            <v-icon>swap_horiz</v-icon>
          </v-btn>
          <v-btn fab small @click.native='togglePause'>
            <v-icon>{{ paused ? "pause_circle_outline" : "play_circle_outline" }}</v-icon>
          </v-btn>
          <v-btn fab small class='red' @click.native='confirmDelete=true'>
            <v-icon>delete</v-icon>
          </v-btn>
        </v-speed-dial>
      </v-flex>
      <!-- title -->
      <v-flex>
        <v-card-title primary-title class='pb-0 pt-3' :class='{ faded: fab }' style='position: relative; transition: all .3s ease; left: 5px;'>
          <p class='headline mb-1'>
            {{ client.stream.name }}
          </p>
          <div class='caption'> <span class='grey--text text--darkenx'><code class='grey darken-2 white--text'>{{ client.stream.streamId }}</code> {{paused ? "(paused)" : ""}} Last updated:
              <timeago :auto-update='10' :since='client.lastUpdate'></timeago></span>
          </div>
        </v-card-title>
      </v-flex>
    </v-layout>
    <v-progress-linear height='1' :indeterminate='true' v-if='client.isLoading'></v-progress-linear>
    <!-- expired alert -->
    <v-alert color='info' v-model='client.expired'>
      <v-layout align-center>
        <v-flex>There are updates available.</v-flex>
        <v-flex>
          <v-btn dark small fab @click.native='refreshStream'>
            <v-icon>refresh</v-icon>
          </v-btn>
        </v-flex>
      </v-layout>
    </v-alert>
    <!-- error alert -->
    <v-alert color='error' v-model='hasError' dismissible>
      <v-layout align-center>
        <v-flex>Error: {{ client.error }}</v-flex>
      </v-layout>
    </v-alert>
    <!-- standard actions -->
    <v-card-actions v-show='true' class='pl-2'>
      <v-spacer></v-spacer>
      <v-btn icon @click.native='toggleLayers' small>
        <v-icon class='xs-actions'>{{ showLayers ? 'keyboard_arrow_up' : 'layers' }}</v-icon>
      </v-btn>
      <!-- <v-btn icon @click.native='toggleLog' small>
          <v-icon class='xs-actions'>{{ showLog ? 'keyboard_arrow_up' : 'list' }}</v-icon>
        </v-btn> -->
      <v-btn icon @click.native='toggleChildren' small>
        <v-icon class='xs-actions'>{{ showChildren ? 'keyboard_arrow_up' : 'history' }}</v-icon>
      </v-btn>
    </v-card-actions>
    <!-- layers -->
    <v-slide-y-transition>
      <div v-show='showLayers' class='pa-0'>
        <sender-layers :layers='client.stream.layers' :objects='client.stream.objects' :clientId='client.ClientId'></sender-layers>
      </div>
    </v-slide-y-transition>
    <!-- log -->
    <v-slide-y-transition>
      <v-card-text v-show='showLog' class='pa-0'>
        <!-- <blockquote class='section-title'>Log</blockquote> -->
        <div class='caption pa-2'>Client id: <code>{{client.ClientId}}</code></div>
        <div class='log pa-2'>
          <template v-for='log in client.log'>
            <div class='caption' mb-5>
              <v-divider></v-divider>
              {{ log.timestamp }}: {{ log.message }}
            </div>
          </template>
        </div>
        <br>
      </v-card-text>
    </v-slide-y-transition>
    <!-- history -->
    <v-slide-y-transition>
      <v-card-text v-show='showChildren' xxxclass='grey darken-4'>
       History: todo
      </v-card-text>
    </v-slide-y-transition>
    <!-- add objects dialog -->
    <v-dialog v-model='showAddDialog'>
      <v-card v-if='objectSelection.length > 0'>
        <v-card-title class="headline">Add {{ selectionObjectCount }} object{{selectionObjectCount > 1 ? "s" : "" }} to the stream?</v-card-title>
        <div>
          <v-layout class='text-xs-center pa-0 ma-0'>
            <v-flex>
              <v-tooltip bottom>
                <span>You can still edit your selection.</span>
                <v-btn block slot='activator' class='light-blue fat-one pa-0 ma-0' @click.native='addObjectsToStream'>Yes!</v-btn>
              </v-tooltip>
            </v-flex>
          </v-layout>
        </div>
      </v-card>
      <v-card v-else class='elevation-4 pa-2'>
        <div class='pa-2 subheading'>
          <v-icon>warning</v-icon>
          No selection found. Please select some objects to add or remove!
        </div>
      </v-card>
    </v-dialog>
    <!-- remove objects dialog -->
    <v-dialog v-model='showRemoveDialog'>
      <v-card v-if='objectSelection.length > 0'>
        <v-card-title class="headline">Remove {{ selectionObjectCount }} object{{selectionObjectCount > 1 ? "s" : "" }} from the stream?</v-card-title>
        <div>
          <v-layout class='text-xs-center pa-0 ma-0'>
            <v-flex>
              <v-tooltip bottom>
                <span>You can still edit your selection.</span>
                <v-btn block slot='activator' class='light-blue fat-one pa-0 ma-0' @click.native='removeObjectsFromStream'>Yes!</v-btn>
              </v-tooltip>
            </v-flex>
          </v-layout>
        </div>
      </v-card>
      <v-card v-else class='elevation-4 pa-2'>
        <div class='pa-2 subheading'>
          <v-icon>warning</v-icon>
          No selection found. Please select some objects to add or remove!
        </div>
      </v-card>
    </v-dialog>
    <!-- confirm delete dialog -->
    <v-dialog v-model='confirmDelete'>
      <v-card>
        <v-card-title class='headline'>Are you sure you want to delete this sender?</v-card-title>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn flat @click.native='confirmDelete=false'>Cancel</v-btn>
          <v-btn color='red' class='' @click.native='removeClient'>Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>
<script>
import SenderLayers from './SenderLayers.vue'

export default {
  name: 'Sender',
  props: {
    client: Object
  },
  components: {
    SenderLayers
  },
  computed: {
    objectSelection( ) { return this.$store.getters.selection },
    selectionObjectCount( ) {
      let sum = 0
      this.objectSelection.forEach( l => sum += l.objectCount )
      return sum
    },
    layerInfo( ) { return this.$store.getters.layerInfo },
  },
  data( ) {
    return {
      fab: false,
      confirmDelete: false,
      showLayers: false,
      showLog: false,
      showChildren: false,
      showMenu: false,
      showAddDialog: false,
      showRemoveDialog: false,
      paused: false,
      hasError: true
    }
  },
  methods: {
    addObjectsToStream( ) {
      this.showAddDialog = false
    },
    removeObjectsFromStream( ) {
      this.showRemoveDialog = false
    },
    togglePause( ) {
      this.paused = !this.paused
    },
    toggleLog( ) {
      if ( this.showLog ) return this.showLog = false
      this.showLog = true
      this.showLayers = false
      this.showChildren = false
    },
    toggleLayers( ) {
      if ( this.showLayers ) return this.showLayers = false
      this.showLayers = true
      this.showLog = false
      this.showChildren = false
    },
    toggleChildren( ) {
      if ( this.showChildren ) return this.showChildren = false
      this.showLayers = false
      this.showLog = false
      this.showChildren = true
    },
    removeClient( ) {
      this.$store.dispatch( 'removeClient', { clientId: this.client.ClientId } )
    }
  },
  mounted( ) {}
}
</script>
<style lang='scss'>
.faded {
  opacity: 0.2
}

.stream-menu {
  position: absolute;
}

.fat-one {
  /*width:100%;*/
}

.make-me-small {
  font-size: 15px !important;
}
</style>